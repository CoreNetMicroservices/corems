/**
 * Centralized route paths for the entire application
 * Exported separately to avoid Fast Refresh warnings
 */

export const APP_ROUTES = {
  // Core app routes
  HOME: "/",
  WELCOME: "/welcome",
  PRICING: "/pricing",
  FEATURES: "/features",

  // User module routes
  LOGIN: "/login",
  FORGOT_PASSWORD: "/forgot-password",
  RESET_PASSWORD: "/reset-password",
  VERIFY_EMAIL: "/verify-email",
  VERIFY_PHONE: "/verify-phone",
  USER_PROFILE: "/profile",
  USERS_LIST: "/users",
  USER_ADD: "/users/add",
  USER_EDIT: "/users/:userId",

  // Communication module routes
  COMMUNICATION: "/communication",
  
  // Translation module routes
  TRANSLATIONS: "/translations",
  TRANSLATION_NEW: "/translations/new",
  TRANSLATION_EDIT: "/translations/:realm/:lang",

  // Template module routes
  TEMPLATES: "/templates",
  TEMPLATE_NEW: "/templates/new",
  TEMPLATE_EDIT: "/templates/:id",

  // Document module routes
  DOCUMENTS: "/documents"
} as const;

// Re-export with different name for internal use
export const ROUTE_PATHS = APP_ROUTES;
