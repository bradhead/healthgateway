import {
    selectorShouldBeVisible,
    getPcrTestTakenTime,
    selectOption,
} from "../../../support/utils";
const { AuthMethod } = require("../../../support/constants");

const pcrTestUrl = "/pcrtest";
const HDID = "P6FFO433A5WPMVTGM7T4ZVWBKCSVNAYGTWTU3J2LWMGUMERKI72A";

// data test id for all input fields in the form
const testKitCodeInput = "[data-testid=test-kit-code-input]";
const testTakenMinutesAgo = "[data-testid=test-taken-minutes-ago]";
const cancelBtn = "[data-testid=btn-cancel]";
const registerKitBtn = "[data-testid=btn-register-kit]";
const pcrPrivacyStatement = "[data-testid=pcr-privacy-statement]";
const registrationSuccessBanner = "[data-testid=registration-success-banner]";
const backToHomeBtn = "[data-testid=btn-continue]";
const errorBanner = "[data-testid=errorBanner]";

// data test id for input required validations
const feedbackTestKitCodeIsRequiredSelector =
    "[data-testid=feedback-testkitcode-is-required]";
const feedbackTestTakenIsRequiredSelector =
    "[data-testid=feedback-testtaken-is-required]";

// data test id for input invalid validations
const feedbackTestKitCodeValidSelector =
    "[data-testid=feedback-testkitcode-is-invalid]";

function clickRegisterKitButton() {
    cy.get("[data-testid=btn-register-kit]")
        .should("be.enabled", "be.visible")
        .click();
}

describe("Authenticated Pcr Test Registration", () => {
    beforeEach(() => {
        cy.enableModules("PcrTest");
        cy.login(
            Cypress.env("keycloak.username"),
            Cypress.env("keycloak.password"),
            AuthMethod.KeyCloak,
            "/home"
        );
        cy.visit(pcrTestUrl);
        cy.intercept("POST", `**/v1/api/Laboratory/${HDID}/LabTestKit`, {
            fixture: "LaboratoryService/authenticatedPcrTest.json",
        });
    });

    it("Authenticated PcrTest Registration Form", () => {
        selectorShouldBeVisible(testKitCodeInput);
        selectorShouldBeVisible(testTakenMinutesAgo);
        selectorShouldBeVisible(cancelBtn);
        selectorShouldBeVisible(registerKitBtn);
        selectorShouldBeVisible(pcrPrivacyStatement);
    });

    it("Required Validations", () => {
        clickRegisterKitButton();
        selectorShouldBeVisible(feedbackTestKitCodeIsRequiredSelector);
        selectorShouldBeVisible(feedbackTestTakenIsRequiredSelector);
    });

    it("Test Kit Code Invalid Validations", () => {
        cy.get(testKitCodeInput).type("111");
        clickRegisterKitButton();
        selectorShouldBeVisible(feedbackTestKitCodeValidSelector);
    });

    it("Successful Test Kit", () => {
        // get the data in the fixture.
        cy.fixture("LaboratoryService/authenticatedPcrTest.json").then(
            function (data) {
                cy.get(testKitCodeInput).type(
                    data.resourcePayload.shortCodeFirst +
                        "-" +
                        data.resourcePayload.shortCodeSecond
                );
                selectOption(
                    testTakenMinutesAgo,
                    getPcrTestTakenTime(
                        data.resourcePayload.testTakenMinutesAgo
                    )
                );
            }
        );
        clickRegisterKitButton();
        selectorShouldBeVisible(registrationSuccessBanner);
        selectorShouldBeVisible(backToHomeBtn);
        cy.get(backToHomeBtn).click();
        cy.url().should("include", Cypress.config("baseUrl"));
    });
});

describe("Authenticated Pcr Test Registration with Error", () => {
    beforeEach(() => {
        cy.enableModules("PcrTest");
        cy.login(
            Cypress.env("keycloak.username"),
            Cypress.env("keycloak.password"),
            AuthMethod.KeyCloak,
            "/home"
        );
        cy.visit(pcrTestUrl);
        cy.intercept("POST", `**/v1/api/Laboratory/${HDID}/LabTestKit`, {
            fixture: "LaboratoryService/authenticatedPcrTestError.json",
        });
    });

    it("Error Registration Test Kit", () => {
        // get the data in the fixture.
        cy.fixture("LaboratoryService/authenticatedPcrTestError.json").then(
            function (data) {
                cy.get(testKitCodeInput).type(
                    data.resourcePayload.shortCodeFirst +
                        "-" +
                        data.resourcePayload.shortCodeSecond
                );
                selectOption(
                    testTakenMinutesAgo,
                    getPcrTestTakenTime(
                        data.resourcePayload.testTakenMinutesAgo
                    )
                );
            }
        );
        clickRegisterKitButton();
        selectorShouldBeVisible(errorBanner);
    });
});
