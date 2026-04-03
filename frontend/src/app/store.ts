import { configureStore } from '@reduxjs/toolkit';
import { baseApi } from '../shared/api/baseApi';

/**
 * Root Redux store for the Muntada frontend.
 * Uses Redux Toolkit with RTK Query for server-state management.
 */
export const store = configureStore({
  reducer: {
    [baseApi.reducerPath]: baseApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(baseApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
