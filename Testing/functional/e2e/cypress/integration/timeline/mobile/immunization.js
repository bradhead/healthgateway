const { AuthMethod } = require("../../../support/constants");
describe("Immunization Async", () => {
  beforeEach(() => {
    let isLoading = false;
    cy.enableModules("Immunization");
    cy.intercept("GET", "**/v1/api/Immunization/*", (req) => {
      if (!isLoading) {
        req.reply({ fixture: "ImmunizationService/immunizationrefresh.json" });
      } else {
        req.reply({ fixture: "ImmunizationService/immunization.json" });
      }
      isLoading = !isLoading;
    });
    cy.viewport("iphone-6");
    cy.login(
      Cypress.env("keycloak.username"),
      Cypress.env("keycloak.password"),
      AuthMethod.KeyCloak
    );
    cy.checkTimelineHasLoaded();
  });

  it("Validate Immunization Loading", () => {
    cy.get("[data-testid=immunizationLoading]")
      .should("be.visible")
      .contains("Still searching for immunization records");
    cy.get("[data-testid=immunizationLoading]").should("not.exist");
    cy.get("[data-testid=immunizationReady]")
      .should("be.visible")
      .find("[data-testid=immunizationBtnReady]")
      .should("be.visible")
      .click();
  });
});

describe("Immunization", () => {
  beforeEach(() => {
    cy.enableModules("Immunization");
    cy.intercept("GET", "**/v1/api/Immunization/*", {
      fixture: "ImmunizationService/immunization.json",
    });
    cy.viewport("iphone-6");
    cy.login(
      Cypress.env("keycloak.username"),
      Cypress.env("keycloak.password"),
      AuthMethod.KeyCloak
    );
    cy.checkTimelineHasLoaded();
  });

  it("Validate Card Details on Mobile", () => {
    cy.get("[data-testid=timelineCard]")
      .first()
      .click()
      .then(() => {
        cy.get("[data-testid=entryDetailsModal]").should("exist");
        cy.get("[data-testid=backBtn]").should("be.visible");
        cy.get("[data-testid=entryCardDetailsTitle]").should("be.visible");
        cy.get("[data-testid=entryCardDate]").should("be.visible");
        cy.get("[data-testid=immunizationProductTitle]").should("be.visible");
        cy.get("[data-testid=immunizationProviderTitle]").should("be.visible");
        cy.get("[data-testid=immunizationLotTitle]").should("be.visible");
        cy.get("[data-testid=cardBtn]").should("be.visible");
      });
  });
});
