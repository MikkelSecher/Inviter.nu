import { BrowserRouter, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { CreateEventPage } from './pages/CreateEventPage';
import { InvitePage } from './pages/InvitePage';
import { ManagePage } from './pages/ManagePage';
import { MyEventsPage } from './pages/MyEventsPage';
import { NotFoundPage } from './pages/NotFoundPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<CreateEventPage />} />
          <Route path="mine" element={<MyEventsPage />} />
          <Route path="invite/:token" element={<InvitePage />} />
          <Route path="manage/:token" element={<ManagePage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
