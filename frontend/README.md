# Core Microservices Frontend

> **Part of [Core Microservices Project](https://github.com/CoreWebMicroservices/corems-project)** - Enterprise-grade microservices toolkit for rapid application development

**Enterprise-grade React application demonstrating modular architecture patterns and best practices**

This project showcases a comprehensive **modular and composable frontend architecture** built with React, TypeScript, and Vite. It demonstrates real-world patterns for building scalable, maintainable web applications that can grow with your business needs.

## What This Project Is

**Core Microservices Frontend** is a full-featured example application that demonstrates:

- **Modular Architecture**: Each business domain (user management, communication, etc.) is a standalone, reusable module
- **Component Composition**: Build complex pages by mixing components from different modules
- **Enterprise Authentication**: JWT + OAuth2 (Google, GitHub, LinkedIn) with role-based access control
- **Advanced Data Management**: React Query for server state, Hookstate for client state
- **Type Safety**: End-to-end TypeScript with strict type checking
- **Professional UI**: React Bootstrap with consistent design system

## Architecture Highlights

### **Modular & Composable Design**

```typescript
// Example: Composing different modules on one page
<UsersPage>
  <UserList /> {/* from user module */}
  <StatsGraph /> {/* from analytics module */}
  <ActivityFeed /> {/* from communication module */}
</UsersPage>
```

### **Centralized Configuration**

- All routes defined in `/app/router/`
- Configurable auth guards and common components
- No hardcoded dependencies between modules
- Easy to add/remove modules without breaking changes

### **Enterprise Features**

- **Multi-provider OAuth2**: Google, GitHub, LinkedIn integration
- **Role-based Access Control**: Admin, user, and service-specific roles
- **Admin Panel**: User management, role assignment, bulk operations
- **Advanced Tables**: Sorting, filtering, pagination, search
- **Responsive Design**: Mobile-first Bootstrap implementation

## Quick Start

```bash
# Clone the main project
git clone https://github.com/CoreWebMicroservices/corems-project.git
cd corems-project

# Navigate to frontend
cd repos/frontend

# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build
```

## Tech Stack

- **Frontend**: React 18+ with TypeScript
- **Build Tool**: Vite for lightning-fast development
- **Routing**: React Router v6+ with configurable auth guards
- **State Management**: React Query + Hookstate
- **UI Framework**: React Bootstrap with custom components
- **API Layer**: Axios with centralized error handling
- **Authentication**: JWT tokens with automatic refresh

## Project Structure

```
src/
├── app/                  # App-level composition & configuration
│   ├── router/           # Centralized routing (routes, guards, config)
│   ├── layout/           # App-wide layout components
│   └── style/            # Global styles
│
├── common/               # Shared, reusable utilities & components
│   ├── component/        # DataTable, Breadcrumb, ApiAlert, etc.
│   ├── router/           # Configurable auth guards
│   └── utils/            # API layer, error handlers, helpers
│
├── user/                 # User module (standalone & composable)
│   ├── component/        # Auth, profile, admin components
│   ├── store/            # User state management
│   ├── model/            # TypeScript interfaces
│   └── config.ts         # Module configuration
│
└── communication/        # Communication module (expandable)
    └── ...               # Similar modular structure
```

## Key Features Demonstrated

### **Authentication & Authorization**

- Multiple OAuth2 providers (Google, GitHub, LinkedIn)
- JWT token management with automatic refresh
- Role-based route protection (`SUPER_ADMIN`, `USER_MS_ADMIN`, etc.)
- Configurable auth guards that can be reused across different apps

### **User Management**

- Complete user CRUD operations
- Admin dashboard with advanced filtering and search
- Role management with intuitive UI
- Profile management with avatar support
- Password change functionality

### **Advanced UI Components**

- **DataTable**: Reusable table with sorting, filtering, pagination, search
- **Breadcrumb Navigation**: Dynamic breadcrumbs with proper routing
- **Form Handling**: React Hook Form with validation and error display
- **Message Management**: Auto-hiding success/error alerts with timer reset

### **Developer Experience**

- Hot reload with Vite
- Comprehensive TypeScript coverage
- ESLint + Prettier for code consistency
- Detailed GitHub Copilot instructions for team development

## Learning Outcomes

This project is perfect for developers who want to learn:

- **Enterprise React Architecture**: How to structure large-scale React applications
- **Modular Design Patterns**: Creating reusable, composable frontend modules
- **Authentication Flows**: Implementing secure JWT + OAuth2 authentication
- **State Management**: Combining React Query and Hookstate effectively
- **TypeScript Best Practices**: End-to-end type safety in React applications
- **Component Composition**: Building flexible UIs through composition over inheritance

## Related Projects

- **Backend**: [Core Microservices](https://github.com/CoreWebMicroservices/corems-project) - Java Spring Boot microservices architecture

## License

Licensed under the Apache License 2.0. See the [main project](https://github.com/CoreWebMicroservices/corems-project) for details.

---

## Contact

Have questions or want to discuss this architecture? Feel free to reach out:

**Dima Mishchenko** - [dmi.mishchenko@gmail.com](mailto:dmi.mishchenko@gmail.com)

## Contributing

Contributions, issues, and feature requests are welcome! See our [Contributing Guidelines](.github/CONTRIBUTING.md) for details.

Built with ❤️ as a demonstration of modern React architecture patterns.

---

## Keywords

`react` `typescript` `vite` `microservices` `modular-architecture` `enterprise` `scalable` `component-composition` `oauth2` `jwt` `role-based-access-control` `react-query` `hookstate` `bootstrap` `admin-panel` `user-management` `document-management` `authentication` `frontend-architecture` `reusable-components`
