import { useEffect, useState } from "react";

const THEME_KEY = "theme";

export const useTheme = () => {
  const [theme, setTheme] = useState(
    () => localStorage.getItem(THEME_KEY) || "light"
  );

  useEffect(() => {
    document.body.setAttribute("data-bs-theme", theme);
    localStorage.setItem(THEME_KEY, theme);
  }, [theme]);

  return { theme, setTheme };
};
