import { RegistrationStatus } from "@/constants/registrationStatus";
import {
    ExternalConfiguration,
    IdentityProviderConfiguration,
    OpenIdConnectConfiguration,
    WebClientConfiguration,
} from "@/models/configData";
import { LoadStatus } from "@/models/storeOperations";
import {
    ConfigActions,
    ConfigGetters,
    ConfigModule,
    ConfigMutations,
    ConfigState,
} from "@/store/modules/config/types";

import { stubbedPromise, stubbedVoid } from "../../utility/stubUtil";

const configState: ConfigState = {
    statusMessage: "",
    config: new ExternalConfiguration(),
    error: false,
    status: LoadStatus.NONE,
};

const configGetters: ConfigGetters = {
    identityProviders(): IdentityProviderConfiguration[] {
        return [];
    },
    openIdConnect(): OpenIdConnectConfiguration {
        return {
            authority: "",
            audience: "",
            clientId: "",
            responseType: "",
            scope: "",
            callbacks: {},
        };
    },
    webClient(): WebClientConfiguration {
        return {
            logLevel: "",
            timeouts: { idle: 0, logoutRedirect: "", resendSMS: 1 },
            registrationStatus: RegistrationStatus.Open,
            externalURLs: {},
            modules: { Note: true },
            hoursForDeletion: 1,
            minPatientAge: 16,
            maxDependentAge: 12,
        };
    },
    isOffline(): boolean {
        return false;
    },
};

const configActions: ConfigActions = {
    initialize: stubbedPromise,
};

const configMutations: ConfigMutations = {
    configurationRequest: stubbedVoid,
    configurationLoaded: stubbedVoid,
    configurationError: stubbedVoid,
};

const configStub: ConfigModule = {
    state: configState,
    namespaced: true,
    getters: configGetters,
    actions: configActions,
    mutations: configMutations,
};

export default configStub;
