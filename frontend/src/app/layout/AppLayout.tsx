import { PropsWithChildren, useState } from "react";
import { Outlet, useLocation } from "react-router-dom";

import AppHeader from "@/app/layout/AppHeader";
import AppSidebar from "@/app/layout/AppSidebar";
const AppLayout = ({ children }: PropsWithChildren) => {
  const location = useLocation();  const [showSidebar, setShowSidebar] = useState(false);

  const isLoginPage = location.pathname === '/login';
  const isForgotPasswordPage = location.pathname === '/forgot-password';
  const isHomePage = location.pathname === '/';

  if (isLoginPage || isForgotPasswordPage || isHomePage) {
    return (
      <div className="mb-5">
        <AppHeader />
        <div className="content" style={{ marginTop: '56px' }}>{children ?? <Outlet />}</div>
      </div>
    );
  }

  return (
    <div>
      <AppHeader onToggleSidebar={() => setShowSidebar(true)} />
      <div className="d-flex" style={{ marginTop: '56px' }}>
        <AppSidebar show={showSidebar} onHide={() => setShowSidebar(false)} />
        <div className="flex-grow-1 p-4">
          {children ?? <Outlet />}
        </div>
      </div>
    </div>
  );
};

export default AppLayout;
