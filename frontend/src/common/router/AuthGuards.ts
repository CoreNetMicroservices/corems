import { redirect } from "react-router-dom";
import { AuthGuards, AuthGuardsConfig } from "./RouterTypes";
import { AppRoles } from "@/common/AppRoles";

/**
 * Shared authentication guards that can be used to validate roles and
 * authentication status before allowing access to routes.
 */
export const createAuthGuards = (config: AuthGuardsConfig): AuthGuards => {
  const redirectIfAuthenticated = () => {
    const { isAuthenticated } = config.authFunctions.getCurrentUserAuth();
    if (isAuthenticated) {
      return redirect(config.homeRoute);
    }
    return true;
  };

  const redirectIfNotAuthenticated = () => {
    const { isAuthenticated } = config.authFunctions.getCurrentUserAuth();
    if (!isAuthenticated) {
      return redirect(config.loginRoute);
    }
    return true;
  };

  const redirectIfNotInRole = (requiredRoles: AppRoles[]) => {
    return () => {
      // Get current user authentication state
      const { isAuthenticated } = config.authFunctions.getCurrentUserAuth();

      if (!isAuthenticated) {
        return redirect(config.loginRoute);
      }

      // Check if user has at least one of the required roles
      const hasRequiredRole = config.authFunctions.hasAnyRole(requiredRoles);

      if (!hasRequiredRole) {
        console.log("User does not have required role, redirecting to home");
        return redirect(config.homeRoute);
      }

      return true;
    };
  };

  // Utility methods for component visibility control
  const isAuthenticated = (): boolean => {
    const { isAuthenticated } = config.authFunctions.getCurrentUserAuth();
    return isAuthenticated;
  };

  const hasAnyRole = (requiredRoles: AppRoles[]): boolean => {
    return config.authFunctions.hasAnyRole(requiredRoles);
  };

  return {
    redirectIfAuthenticated,
    redirectIfNotAuthenticated,
    redirectIfNotInRole,
    isAuthenticated,
    hasAnyRole,
  };
};
