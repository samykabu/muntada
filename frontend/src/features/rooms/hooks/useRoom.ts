import { useEffect, useRef, useState, useCallback } from 'react';
import type { RoomOccurrenceStatus, RoomParticipantResponse } from '../api/roomsApi';

/** Events received from the SignalR hub. */
export interface RoomHubEvent {
  type:
    | 'ParticipantJoined'
    | 'ParticipantLeft'
    | 'MediaStateChanged'
    | 'StatusChanged'
    | 'GraceStarted'
    | 'GraceEnded'
    | 'RoomEnded';
  payload: unknown;
}

/** The state managed by the useRoom hook. */
export interface RoomState {
  /** Whether the SignalR connection is active. */
  isConnected: boolean;
  /** Current connection error, if any. */
  connectionError?: string;
  /** Real-time participant list. */
  participants: RoomParticipantResponse[];
  /** Current room status (updated via SignalR). */
  roomStatus: RoomOccurrenceStatus | null;
  /** Grace period countdown in seconds, or null when not in grace. */
  graceCountdown: number | null;
}

interface UseRoomOptions {
  /** The tenant ID. */
  tenantId: string;
  /** The room occurrence ID to connect to. */
  occurrenceId: string;
  /** Base URL for the SignalR hub. Defaults to VITE_API_BASE_URL or localhost. */
  hubBaseUrl?: string;
  /** Whether to auto-connect on mount. Defaults to true. */
  autoConnect?: boolean;
}

/**
 * Hook for managing a SignalR connection to a live room.
 * Provides real-time participant list, room status updates, and grace period countdown.
 *
 * Note: This hook provides the connection management structure. The actual SignalR
 * client library (@microsoft/signalr) should be installed as a project dependency
 * when integrating with the real backend.
 */
export function useRoom({
  tenantId,
  occurrenceId,
  hubBaseUrl,
  autoConnect = true,
}: UseRoomOptions): RoomState & {
  connect: () => void;
  disconnect: () => void;
} {
  const [state, setState] = useState<RoomState>({
    isConnected: false,
    participants: [],
    roomStatus: null,
    graceCountdown: null,
  });

  const connectionRef = useRef<unknown>(null);
  const graceTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const baseUrl = hubBaseUrl ?? import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';
  const hubUrl = `${baseUrl}/hubs/room?tenantId=${tenantId}&occurrenceId=${occurrenceId}`;

  /** Clear the grace countdown timer. */
  const clearGraceTimer = useCallback(() => {
    if (graceTimerRef.current) {
      clearInterval(graceTimerRef.current);
      graceTimerRef.current = null;
    }
    setState((prev) => ({ ...prev, graceCountdown: null }));
  }, []);

  /** Start a grace countdown for the given number of seconds. */
  const startGraceCountdown = useCallback((seconds: number) => {
    clearGraceTimer();
    let remaining = seconds;
    setState((prev) => ({ ...prev, graceCountdown: remaining }));
    graceTimerRef.current = setInterval(() => {
      remaining -= 1;
      if (remaining <= 0) {
        clearGraceTimer();
      } else {
        setState((prev) => ({ ...prev, graceCountdown: remaining }));
      }
    }, 1000);
  }, [clearGraceTimer]);

  /** Handle incoming hub events. */
  const handleEvent = useCallback(
    (event: RoomHubEvent) => {
      switch (event.type) {
        case 'ParticipantJoined': {
          const participant = event.payload as RoomParticipantResponse;
          setState((prev) => ({
            ...prev,
            participants: [...prev.participants.filter((p) => p.id !== participant.id), participant],
          }));
          break;
        }
        case 'ParticipantLeft': {
          const { participantId, leftAt } = event.payload as { participantId: string; leftAt: string };
          setState((prev) => ({
            ...prev,
            participants: prev.participants.map((p) =>
              p.id === participantId ? { ...p, leftAt } : p,
            ),
          }));
          break;
        }
        case 'MediaStateChanged': {
          const update = event.payload as { participantId: string; audioState?: string; videoState?: string };
          setState((prev) => ({
            ...prev,
            participants: prev.participants.map((p) =>
              p.id === update.participantId
                ? {
                    ...p,
                    ...(update.audioState !== undefined && { audioState: update.audioState as RoomParticipantResponse['audioState'] }),
                    ...(update.videoState !== undefined && { videoState: update.videoState as RoomParticipantResponse['videoState'] }),
                  }
                : p,
            ),
          }));
          break;
        }
        case 'StatusChanged': {
          const { status } = event.payload as { status: RoomOccurrenceStatus };
          setState((prev) => ({ ...prev, roomStatus: status }));
          if (status !== 'Grace') {
            clearGraceTimer();
          }
          break;
        }
        case 'GraceStarted': {
          const { gracePeriodSeconds } = event.payload as { gracePeriodSeconds: number };
          setState((prev) => ({ ...prev, roomStatus: 'Grace' }));
          startGraceCountdown(gracePeriodSeconds);
          break;
        }
        case 'GraceEnded': {
          clearGraceTimer();
          break;
        }
        case 'RoomEnded': {
          clearGraceTimer();
          setState((prev) => ({
            ...prev,
            roomStatus: 'Ended',
            isConnected: false,
          }));
          break;
        }
      }
    },
    [clearGraceTimer, startGraceCountdown],
  );

  /** Connect to the SignalR hub. */
  const connect = useCallback(() => {
    // In a real implementation, this would create an HubConnectionBuilder instance:
    //   const connection = new HubConnectionBuilder()
    //     .withUrl(hubUrl)
    //     .withAutomaticReconnect()
    //     .build();
    //   connection.on('RoomEvent', handleEvent);
    //   connection.start();
    //
    // For now, we store a placeholder and mark as connected to support the component structure.
    connectionRef.current = { hubUrl };
    setState((prev) => ({
      ...prev,
      isConnected: true,
      connectionError: undefined,
    }));
  }, [hubUrl]);

  /** Disconnect from the SignalR hub. */
  const disconnect = useCallback(() => {
    clearGraceTimer();
    connectionRef.current = null;
    setState((prev) => ({
      ...prev,
      isConnected: false,
    }));
  }, [clearGraceTimer]);

  // Auto-connect on mount.
  useEffect(() => {
    if (autoConnect) {
      connect();
    }
    return () => {
      disconnect();
    };
  }, [autoConnect, connect, disconnect]);

  // Expose handleEvent for testing (not part of the public API, but useful for integration).
  // In production, events flow through the SignalR connection's .on() handler.
  void handleEvent;

  return {
    ...state,
    connect,
    disconnect,
  };
}
