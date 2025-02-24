import { AuthMethod } from "../../../support/constants";

const addQuickLinkButtonSelector = "[data-testid=add-quick-link-button]";
const addQuickLinkChipSelector = "[data-testid=quick-link-modal-text] .v-chip";
const addQuickLinkSubmitButtonSelector = "[data-testid=add-quick-link-btn]";
const tooManyRequestsStatusCode = 429;
const serverErrorStatusCode = 500;

function testGetConfigurationError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("GET", "/configuration", {
        statusCode,
    });
    cy.visit("/");

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=app-warning]").should("be.visible");
    } else {
        cy.get("[data-testid=app-error]").should("be.visible");
    }
}

function testGetProfileErrorOnLoad(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/home"
    );
    cy.intercept("GET", "**/UserProfile/*", {
        statusCode,
    });
    cy.reload();

    cy.get("[data-testid=patient-retrieval-error]").should("be.visible");
    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-busy]").should("be.visible");
    } else {
        cy.get("[data-testid=too-busy]").should("not.exist");
    }
}

function testRegisterError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    const hdid = "S22BPV6WHS5TRLBL4XKGQDBVDUKLPIRSBGYSEJAHYMYRP22SP2TA";
    cy.intercept("GET", `**/UserProfile/${hdid}`, {
        fixture: "UserProfileService/userProfileUnregistered.json",
    });
    cy.intercept("GET", `**/UserProfile/${hdid}/Validate`, {
        body: {
            resourcePayload: true,
            totalResultCount: 1,
            pageIndex: 0,
            pageSize: 1,
            resultStatus: 1, // success
        },
    });
    cy.intercept("POST", `**/UserProfile/${hdid}`, {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.unregistered.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak
    );

    cy.location("pathname").should("eq", "/registration");
    cy.get("[data-testid=emailCheckbox] input")
        .should("be.enabled")
        .check({ force: true });
    cy.get("[data-testid=emailInput]")
        .should("be.visible")
        .find("input")
        .should("be.enabled")
        .type(Cypress.env("emailAddress"));
    cy.get("[data-testid=emailConfirmationInput]")
        .should("be.visible")
        .find("input")
        .should("be.enabled")
        .type(Cypress.env("emailAddress"));
    cy.get("[data-testid=smsNumberInput]")
        .should("be.visible")
        .find("input")
        .should("be.enabled")
        .type(Cypress.env("phoneNumber"));
    cy.get("[data-testid=acceptCheckbox] input")
        .should("be.enabled")
        .check({ force: true })
        .wait(500);
    cy.get("[data-testid=registerButton]")
        .should("be.visible", "be.enabled")
        .click();

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function testValidateEmailError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("GET", "**/UserProfile/*/email/validate/dummyinvitekey", {
        statusCode,
    }).as("validateEmail");
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/validateEmail/dummyinvitekey"
    );

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function testAddQuickLinkError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({
        datasets: [
            {
                name: "covid19TestResult",
                enabled: true,
            },
            {
                name: "labResult",
                enabled: true,
            },
            {
                name: "healthVisit",
                enabled: true,
            },
            {
                name: "hospitalVisit",
                enabled: true,
            },
            {
                name: "immunization",
                enabled: true,
            },
            {
                name: "medication",
                enabled: true,
            },
            {
                name: "specialAuthorityRequest",
                enabled: true,
            },
            {
                name: "note",
                enabled: true,
            },
        ],
    });

    cy.intercept("PUT", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.intercept("POST", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/home"
    );

    cy.get(addQuickLinkButtonSelector)
        .should("be.visible")
        .should("be.enabled")
        .click();
    cy.get(`${addQuickLinkChipSelector}[name=Laboratory-filter]`)
        .should("exist")
        .click();
    cy.get(addQuickLinkSubmitButtonSelector)
        .should("be.visible")
        .should("be.enabled")
        .click()
        .should("be.disabled");

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=quick-link-modal-error]").should("be.visible");
    }
}

function testAddCommentError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({
        timeline: {
            comment: true,
        },
        datasets: [
            {
                name: "covid19TestResult",
                enabled: true,
            },
        ],
    });
    cy.intercept("POST", "**/UserProfile/*/Comment", {
        statusCode,
    });
    cy.intercept("GET", "**/Laboratory/LaboratoryOrders*", {
        fixture: "LaboratoryService/laboratoryOrders.json",
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak
    );
    cy.checkTimelineHasLoaded();

    cy.get("[data-testid=timelineCard]")
        .first()
        .scrollIntoView()
        .click()
        .within(() => {
            cy.get("[data-testid=add-comment-text-area]").type("Test Comment");
            cy.get("[data-testid=post-comment-btn]").click();
        });

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function getQuickLinkCard(title) {
    return cy
        .contains("[data-testid=card-button-title]", title)
        .parents("[data-testid=quick-link-card]");
}

function testRemoveQuickLinkError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({
        datasets: [
            {
                name: "covid19TestResult",
                enabled: true,
            },
            {
                name: "labResult",
                enabled: true,
            },
            {
                name: "healthVisit",
                enabled: true,
            },
            {
                name: "hospitalVisit",
                enabled: true,
            },
            {
                name: "immunization",
                enabled: true,
            },
            {
                name: "medication",
                enabled: true,
            },
            {
                name: "specialAuthorityRequest",
                enabled: true,
            },
            {
                name: "note",
                enabled: true,
            },
        ],
    });
    cy.intercept("PUT", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.intercept("POST", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/home"
    );

    getQuickLinkCard("Medications").within(() => {
        cy.get("[data-testid=card-menu-button]")
            .should("be.visible", "be.enabled")
            .click();
        cy.document()
            .find("[data-testid=remove-quick-link-button]")
            .should("be.visible")
            .click();
    });

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function testHideVaccineCardQuickLinkError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("PUT", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.intercept("POST", "**/UserProfile/*/preference", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/home"
    );

    cy.get("[data-testid=bc-vaccine-card-card]").within(() => {
        cy.get("[data-testid=card-menu-button]")
            .should("be.visible", "be.enabled")
            .click();
        cy.document()
            .find("[data-testid=remove-quick-link-button]")
            .should("be.visible")
            .click();
    });

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function testEditSmsError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("GET", "**/UserProfile/*", {
        fixture: "UserProfileService/userProfile.json",
    });
    cy.intercept("GET", "**/UserProfile/IsValidPhoneNumber/*", {
        body: true,
    });
    cy.intercept("PUT", "**/UserProfile/*/sms", {
        statusCode,
    });
    cy.intercept("POST", "**/UserProfile/*/sms", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/profile"
    );

    cy.get("[data-testid=editSMSBtn]").click();
    cy.get("[data-testid=smsNumberInput] input").clear().type("2506714848");
    cy.get("[data-testid=saveSMSEditBtn]").click();

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

function testVerifySmsError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("GET", "**/UserProfile/IsValidPhoneNumber/*", {
        body: true,
    });
    cy.intercept("GET", "**/UserProfile/*/sms/validate/*", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/profile"
    );

    cy.get("[data-testid=verifySMSBtn]")
        .should("be.visible", "be.enabled")
        .click();

    cy.get("[data-testid=verifySMSModalCodeInput]")
        .should("be.visible")
        .find("input")
        .should("have.focus")
        .type("123456");

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=verifySMSModalUnexpectedErrorText]").should(
            "be.visible"
        );
    }
}

function testEditEmailError(statusCode = serverErrorStatusCode) {
    cy.configureSettings({});
    cy.intercept("GET", "**/UserProfile/*", {
        fixture: "UserProfileService/userProfile.json",
    });
    cy.intercept("GET", "**/UserProfile/IsValidPhoneNumber/*", {
        body: true,
    });
    cy.intercept("PUT", "**/UserProfile/*/email", {
        statusCode,
    });
    cy.intercept("POST", "**/UserProfile/*/email", {
        statusCode,
    });
    cy.login(
        Cypress.env("keycloak.username"),
        Cypress.env("keycloak.password"),
        AuthMethod.KeyCloak,
        "/profile"
    );

    cy.get("[data-testid=editEmailBtn]").click();
    cy.get("[data-testid=email-input] input").type(Cypress.env("emailAddress"));
    cy.get("[data-testid=editEmailSaveBtn]").click();

    if (statusCode === tooManyRequestsStatusCode) {
        cy.get("[data-testid=too-many-requests-error]").should("be.visible");
    } else {
        cy.get("[data-testid=errorBanner]").should("be.visible");
    }
}

describe("Error Alerts", () => {
    it("Error Retrieving Configuration", () => {
        testGetConfigurationError();
    });

    it("Error Retrieving Profile on Load", () => {
        testGetProfileErrorOnLoad();
    });

    it("Error Registering", () => {
        testRegisterError();
    });

    it("Error Validating Email", () => {
        testValidateEmailError();
    });

    it("Error Adding Quick Link", () => {
        testAddQuickLinkError();
    });

    it("Error Adding Comment", () => {
        testAddCommentError();
    });

    it("Error Removing Quick Link", () => {
        testRemoveQuickLinkError();
    });

    it("Error Hiding Vaccine Card Quick Link", () => {
        testHideVaccineCardQuickLinkError();
    });

    it("Error Editing SMS Number", () => {
        testEditSmsError();
    });

    it("Error On SMS Verification", () => {
        testVerifySmsError();
    });

    it("Error Editing Email", () => {
        testEditEmailError();
    });
});

describe("429 Alerts", () => {
    it("429 Error Retrieving Configuration", () => {
        testGetConfigurationError(429);
    });

    it("429 Error Retrieving Profile on Load", () => {
        testGetProfileErrorOnLoad(429);
    });

    it("429 Error Registering", () => {
        testRegisterError(429);
    });

    it("429 Error Validating Email", () => {
        testValidateEmailError(429);
    });

    it("429 Error Adding Quick Link", () => {
        testAddQuickLinkError(429);
    });

    it("429 Error Adding Comment", () => {
        testAddCommentError(429);
    });

    it("429 Error Removing Quick Link", () => {
        testRemoveQuickLinkError(429);
    });

    it("429 Error Hiding Vaccine Card Quick Link", () => {
        testHideVaccineCardQuickLinkError(429);
    });

    it("429 Error Editing SMS Number", () => {
        testEditSmsError(429);
    });

    it("429 Error On SMS Verification", () => {
        testVerifySmsError(429);
    });

    it("429 Error Editing Email", () => {
        testEditEmailError(429);
    });
});
