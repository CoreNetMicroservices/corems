import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';

interface ScrollToProps {
  x?: number;
  y?: number;
  behavior?: ScrollBehavior;
}

const ScrollTo = ({ x = 0, y = 0, behavior = 'auto' }: ScrollToProps) => {
  const { pathname } = useLocation();

  useEffect(() => {
    window.scrollTo({ top: y, left: x, behavior });
  }, [pathname, x, y, behavior]);

  return null;
};

export default ScrollTo;
