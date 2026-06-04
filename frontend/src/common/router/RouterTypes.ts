import { AppRoles } from "@/common/AppRoles";
import { RouteObject } from "react-router-dom";

// Base interface for all route modules
export interface RouteModule {
  routes: RouteObject[];
  routePaths: Record<string, string>;
}

// Auth function types for dependency injection
export interface AuthFunctions {
  getCurrentUserAuth: () => {
    isAuthenticated: boolean;
    user: unknown; // Will be ImmutableObject<User> from Hookstate
    roles: unknown; // Will be ImmutableArray<string> or string[] from Hookstate
  };
  hasAnyRole: (requiredRoles: AppRoles[]) => boolean;
}

// Configuration for AuthGuards
export interface AuthGuardsConfig {
  homeRoute: string;
  loginRoute: string;
  authFunctions: AuthFunctions;
}

// Auth guard functions interface
export interface AuthGuards {
  // Route guards for navigation
  redirectIfAuthenticated: () => boolean | Response;
  redirectIfNotAuthenticated: () => boolean | Response;
  redirectIfNotInRole: (requiredRoles: AppRoles[]) => () => boolean | Response;

  // Utility methods for component visibility control
  isAuthenticated: () => boolean;
  hasAnyRole: (requiredRoles: AppRoles[]) => boolean;
}

// Route configuration for modules
export interface ModuleRouteConfig {
  prefix?: string;
  guards?: Partial<AuthGuards>;
  routes: RouteObject[];
}
