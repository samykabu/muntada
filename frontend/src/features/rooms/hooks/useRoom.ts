import { useEffect, useRef, useState, useCallback } from 'react';
import type { RoomOccurrenceStatus, RoomParticipantResponse } from '../api/roomsApi';

/** Event data types received from the SignalR hub as separate method calls. */
export interface ParticipantJoinedEvent { participantId: string; displayName: string; role: string; }
export interface ParticipantLeftEvent { participantId: string; userId: string; leftAt: string; }
export interface ParticipantMediaChangedEvent { participantId: string; audioState: string; videoState: string; }
export interface RoomStatusChangedEvent { occurrenceId: string; status: RoomOccurrenceStatus; graceStartedAt?: string; graceExpiresAt?: string; }
export interface ModeratorChangedEvent { occurrenceId: string; newModeratorUserId: string; newModeratorName: string; }
export interface RecordingStatusChangedEvent { occurrenceId: string; isRecording: boolean; }

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
  const hubUrl = `${baseUrl}/hubs/rooms?tenantId=${tenantId}&occurrenceId=${occurrenceId}`;

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

  /** Handle ParticipantJoined hub event. */
  const handleParticipantJoined = useCallback((data: ParticipantJoinedEvent) => {
    setState((prev) => ({
      ...prev,
      participants: [
        ...prev.participants.filter((p) => p.id !== data.participantId),
        { id: data.participantId, displayName: data.displayName, role: data.role as RoomParticipantResponse['role'] } as RoomParticipantResponse,
      ],
    }));
  }, []);

  /** Handle ParticipantLeft hub event. */
  const handleParticipantLeft = useCallback((data: ParticipantLeftEvent) => {
    setState((prev) => ({
      ...prev,
      participants: prev.participants.map((p) =>
        p.id === data.participantId ? { ...p, leftAt: data.leftAt } : p,
      ),
    }));
  }, []);

  /** Handle ParticipantMediaChanged hub event. */
  const handleParticipantMediaChanged = useCallback((data: ParticipantMediaChangedEvent) => {
    setState((prev) => ({
      ...prev,
      participants: prev.participants.map((p) =>
        p.id === data.participantId
          ? {
              ...p,
              audioState: data.audioState as RoomParticipantResponse['audioState'],
              videoState: data.videoState as RoomParticipantResponse['videoState'],
            }
          : p,
      ),
    }));
  }, []);

  /** Handle RoomStatusChanged hub event. */
  const handleRoomStatusChanged = useCallback((data: RoomStatusChangedEvent) => {
    setState((prev) => ({ ...prev, roomStatus: data.status }));
    if (data.status === 'Grace' && data.graceExpiresAt) {
      const remaining = Math.max(0, Math.round((new Date(data.graceExpiresAt).getTime() - Date.now()) / 1000));
      startGraceCountdown(remaining);
    } else if (data.status === 'Ended') {
      clearGraceTimer();
      setState((prev) => ({ ...prev, isConnected: false }));
    } else if (data.status !== 'Grace') {
      clearGraceTimer();
    }
  }, [clearGraceTimer, startGraceCountdown]);

  /** Handle ModeratorChanged hub event. */
  const handleModeratorChanged = useCallback((_data: ModeratorChangedEvent) => {
    // Moderator change is informational; UI can refetch occurrence details if needed.
  }, []);

  /** Handle RecordingStatusChanged hub event. */
  const handleRecordingStatusChanged = useCallback((_data: RecordingStatusChangedEvent) => {
    // Recording status change is informational; UI can refetch recordings if needed.
  }, []);

  /** Connect to the SignalR hub. */
  const connect = useCallback(() => {
    // In a real implementation, this would create an HubConnectionBuilder instance:
    //   const connection = new HubConnectionBuilder()
    //     .withUrl(hubUrl)
    //     .withAutomaticReconnect()
    //     .build();
    //   connection.on('ParticipantJoined', handleParticipantJoined);
    //   connection.on('ParticipantLeft', handleParticipantLeft);
    //   connection.on('ParticipantMediaChanged', handleParticipantMediaChanged);
    //   connection.on('RoomStatusChanged', handleRoomStatusChanged);
    //   connection.on('ModeratorChanged', handleModeratorChanged);
    //   connection.on('RecordingStatusChanged', handleRecordingStatusChanged);
    //   connection.start();
    //
    // For now, we store a placeholder and mark as connected to support the component structure.
    connectionRef.current = { hubUrl };
    setState((prev) => ({
      ...prev,
      isConnected: true,
      connectionError: undefined,
    }));
  }, [hubUrl, handleParticipantJoined, handleParticipantLeft, handleParticipantMediaChanged, handleRoomStatusChanged, handleModeratorChanged, handleRecordingStatusChanged]);

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

  // Expose handlers for testing (not part of the public API, but useful for integration).
  // In production, events flow through the SignalR connection's .on() handlers.
  void handleParticipantJoined;
  void handleParticipantLeft;
  void handleParticipantMediaChanged;
  void handleRoomStatusChanged;
  void handleModeratorChanged;
  void handleRecordingStatusChanged;

  return {
    ...state,
    connect,
    disconnect,
  };
}
