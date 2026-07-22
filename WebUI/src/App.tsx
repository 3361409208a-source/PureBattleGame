import React, { useState, useEffect } from 'react';
import { SocialHubView } from './components/SocialHub/SocialHubView';

export const App: React.FC = () => {
  const [route, setRoute] = useState<string>(() => window.location.hash.replace('#/', '') || 'social-hub');

  useEffect(() => {
    const handleHashChange = () => {
      const currentRoute = window.location.hash.replace('#/', '') || 'social-hub';
      setRoute(currentRoute);
    };

    window.addEventListener('hashchange', handleHashChange);
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  switch (route) {
    case 'social-hub':
    default:
      return <SocialHubView />;
  }
};

export default App;
