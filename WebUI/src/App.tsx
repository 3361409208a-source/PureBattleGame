import React, { useState, useEffect } from 'react';
import { LauncherHubView } from './components/Launcher/LauncherHubView';
import { SocialHubView } from './components/SocialHub/SocialHubView';

export const App: React.FC = () => {
  const [route, setRoute] = useState<string>(() => {
    const hashRoute = window.location.hash.replace('#/', '');
    if (hashRoute) return hashRoute;
    const urlParams = new URLSearchParams(window.location.search);
    const queryRoute = urlParams.get('route');
    if (queryRoute) return queryRoute;
    return 'launcher';
  });

  useEffect(() => {
    const handleHashChange = () => {
      const currentRoute = window.location.hash.replace('#/', '') || 'launcher';
      setRoute(currentRoute);
    };

    window.addEventListener('hashchange', handleHashChange);
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  switch (route) {
    case 'social-hub':
      return <SocialHubView />;
    case 'launcher':
    default:
      return <LauncherHubView />;
  }
};

export default App;
