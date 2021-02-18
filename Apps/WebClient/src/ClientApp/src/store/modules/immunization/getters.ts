import { GetterTree } from "vuex";

import { ImmunizationEvent, Recommendation } from "@/models/immunizationModel";
import { ImmunizationState, LoadStatus, RootState } from "@/models/storeState";

export const getters: GetterTree<ImmunizationState, RootState> = {
    getStoredImmunizations(state: ImmunizationState): ImmunizationEvent[] {
        return state.immunizations;
    },
    getStoredRecommendations(state: ImmunizationState): Recommendation[] {
        return state.recommendations;
    },
    isDeferredLoad(state: ImmunizationState): boolean {
        return state.status === LoadStatus.DEFERRED;
    },
    isLoading(state: ImmunizationState): boolean {
        return state.status === LoadStatus.REQUESTED;
    },
};
