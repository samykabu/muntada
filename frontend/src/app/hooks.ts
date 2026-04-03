import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from './store';

/** Typed dispatch hook for the Muntada store. */
export const useAppDispatch = useDispatch.withTypes<AppDispatch>();

/** Typed selector hook for the Muntada store. */
export const useAppSelector = useSelector.withTypes<RootState>();
