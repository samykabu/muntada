import { BrowserRouter, Routes, Route } from 'react-router-dom';

/**
 * Root application component with React Router setup.
 * Feature modules will add their routes here as they're built.
 */
function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
      </Routes>
    </BrowserRouter>
  );
}

/** Placeholder home page — replaced by actual UI in later epics. */
function Home() {
  return (
    <div style={{ padding: '2rem', fontFamily: 'system-ui, sans-serif' }}>
      <h1>Muntada</h1>
      <p>Platform is running. Feature modules will be added in subsequent epics.</p>
    </div>
  );
}

export default App;
