import { RouteObject } from "react-router-dom";
import { createAuthGuards } from "@/common/router/AuthGuards";
import { AppRoles } from "@/common/AppRoles";
import { ROUTE_PATHS } from "@/app/router/routes";
import { getCurrentUserAuth, hasAnyRole } from "@/user/store/AuthState";
import { Container, Row, Col, Button } from "react-bootstrap";
import { Link } from "react-router-dom";

// Import components directly from modules (no route configuration)
import AuthForm from "@/user/component/auth/AuthForm";
import ForgotPasswordForm from "@/user/component/auth/ForgotPasswordForm";
import ResetPasswordForm from "@/user/component/auth/ResetPasswordForm";
import VerifyEmailForm from "@/user/component/auth/VerifyEmailForm";
import VerifyPhoneForm from "@/user/component/auth/VerifyPhoneForm";
import UserProfile from "@/user/component/profile/UserProfile";
import UserList from "@/user/component/user/UserList";
import AppUserEdit from "@/app/layout/components/UserEdit";
import UserAdd from "@/user/component/user/UserAdd";
import { MessageList } from "@/communication/component/MessageList";
import TranslationEditor from "@/translation/component/TranslationEditor";
import { TranslationList } from "@/translation/component/TranslationList";
import { TemplateList } from "@/template/component/TemplateList";
import TemplateEditor from "@/template/component/TemplateEditor";
import DocumentList from "@/document/component/DocumentList";
import HomePage from "@/app/pages/HomePage";
import WelcomePage from "@/app/pages/WelcomePage";
import { 
  IntroductionPage, 
  QuickStartPage, 
  ArchitecturePage, 
  RoadmapPage, 
  ServicesPage 
} from "@/app/pages/docs";
import CommonLibrariesPage from "@/app/pages/docs/CommonLibrariesPage";
import UserServicePage from "@/app/pages/docs/services/UserServicePage";
import CommunicationServicePage from "@/app/pages/docs/services/CommunicationServicePage";
import DocumentServicePage from "@/app/pages/docs/services/DocumentServicePage";
import TranslationServicePage from "@/app/pages/docs/services/TranslationServicePage";

// Create auth guards with dependency injection
const authGuards = createAuthGuards({
  homeRoute: ROUTE_PATHS.HOME,
  loginRoute: ROUTE_PATHS.LOGIN,
  authFunctions: {
    getCurrentUserAuth,
    hasAnyRole,
  },
});

/**
 * Centralized route configuration for the entire application
 * This file composes all module components into routes
 */

// Core application routes
export const coreRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.HOME,
    element: <HomePage />,
  },
  {
    path: '/welcome',
    element: <WelcomePage />,
    loader: authGuards.redirectIfNotAuthenticated,
  }
];

// User module routes (moved from UserRouter)
export const userRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.LOGIN,
    element: <AuthForm />,
    loader: authGuards.redirectIfAuthenticated,
  },
  {
    path: ROUTE_PATHS.FORGOT_PASSWORD,
    element: <ForgotPasswordForm />,
    loader: authGuards.redirectIfAuthenticated,
  },
  {
    path: ROUTE_PATHS.RESET_PASSWORD,
    element: <ResetPasswordForm />,
    loader: authGuards.redirectIfAuthenticated,
  },
  {
    path: ROUTE_PATHS.VERIFY_EMAIL,
    element: <VerifyEmailForm />,
  },
  {
    path: ROUTE_PATHS.VERIFY_PHONE,
    element: <VerifyPhoneForm />,
  },
  {
    path: ROUTE_PATHS.USER_PROFILE,
    element: <UserProfile />,
    loader: authGuards.redirectIfNotAuthenticated,
  },
  {
    path: ROUTE_PATHS.USERS_LIST,
    element: <UserList />,
    loader: authGuards.redirectIfNotInRole([AppRoles.UserMsAdmin]),
  },
  {
    path: ROUTE_PATHS.USER_ADD,
    element: <UserAdd />,
    loader: authGuards.redirectIfNotInRole([AppRoles.UserMsAdmin]),
  },
  {
    path: ROUTE_PATHS.USER_EDIT,
    element: <AppUserEdit />,
    loader: authGuards.redirectIfNotInRole([AppRoles.UserMsAdmin]),
  },
];

// Communication module routes
export const communicationRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.COMMUNICATION,
    element: <MessageList />,
    loader: authGuards.redirectIfNotAuthenticated,
  },
];

export const translationRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.TRANSLATIONS,
    element: <TranslationList />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TranslationMsAdmin]),
  },
  {
    path: ROUTE_PATHS.TRANSLATION_EDIT,
    element: <TranslationEditor />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TranslationMsAdmin]),
  },
  {
    path: ROUTE_PATHS.TRANSLATION_NEW,
    element: <TranslationEditor />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TranslationMsAdmin]),
  },
];

export const templateRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.TEMPLATES,
    element: <TemplateList />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TemplateMsAdmin]),
  },
  {
    path: ROUTE_PATHS.TEMPLATE_EDIT,
    element: <TemplateEditor />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TemplateMsAdmin]),
  },
  {
    path: ROUTE_PATHS.TEMPLATE_NEW,
    element: <TemplateEditor />,
    loader: authGuards.redirectIfNotInRole([AppRoles.TemplateMsAdmin]),
  },
];

// Document module routes
export const documentRoutes: RouteObject[] = [
  {
    path: ROUTE_PATHS.DOCUMENTS,
    element: <DocumentList />,
    loader: authGuards.redirectIfNotAuthenticated,
  },
];

// Documentation routes
export const docsRoutes: RouteObject[] = [
  {
    path: '/docs/introduction',
    element: <IntroductionPage />,
  },
  {
    path: '/docs/quick-start',
    element: <QuickStartPage />,
  },
  {
    path: '/docs/architecture',
    element: <ArchitecturePage />,
  },
  {
    path: '/docs/common-libraries',
    element: <CommonLibrariesPage />,
  },
  {
    path: '/docs/roadmap',
    element: <RoadmapPage />,
  },
  {
    path: '/docs/services',
    element: <ServicesPage />,
  },
  {
    path: '/docs/services/user',
    element: <UserServicePage />,
  },
  {
    path: '/docs/services/communication',
    element: <CommunicationServicePage />,
  },
  {
    path: '/docs/services/document',
    element: <DocumentServicePage />,
  },
  {
    path: '/docs/services/translation',
    element: <TranslationServicePage />,
  },
];

// Combined routes for the entire application
export const allApplicationRoutes: RouteObject[] = [
  ...coreRoutes,
  ...userRoutes,
  ...communicationRoutes,
  ...translationRoutes,
  ...templateRoutes,
  ...documentRoutes,
  ...docsRoutes,
  {
    path: '*',
    element: <div>
      <Container className="d-flex flex-column align-items-center justify-content-center mt-5" >
        <Row className="text-center">
          <Col xs={12}>
            <h2 className="d-flex justify-content-center align-items-center gap-2 mb-2">
              <span className="display-1 fw-bold">4</span>
              <i className="bi bi-exclamation-circle-fill text-danger display-4"></i>
              <span className="display-1 fw-bold bsb-flip-h">4</span>
            </h2>
            <h3 className="h2 mb-4">Oops! You're lost.</h3>
            <Link to="/">
              <Button size="lg" type="submit" className="fs-6 p-3">Back To Home</Button>
            </Link>
          </Col>
        </Row>
      </Container>
    </div>,
  },
];