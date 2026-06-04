import { createBrowserRouter, RouteObject, RouterProvider } from "react-router-dom";

import App from "@/app/App";
import NotFound from "@/app/layout/NotFound";
import { allApplicationRoutes } from "@/app/router/AppRoutes";

console.log("Router Init");

const AppRouter = () => {
  // Use centralized route configuration
  const allRoutes = allApplicationRoutes;

  const routes: RouteObject[] = [
    {
      path: "",
      element: <App />,
      errorElement: <NotFound />,
      children: allRoutes,
    },
  ];

  const router = createBrowserRouter(routes);

  return <RouterProvider router={router} />;
};

export default AppRouter;