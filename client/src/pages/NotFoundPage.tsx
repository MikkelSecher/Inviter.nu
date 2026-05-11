import { Link } from 'react-router-dom';
import { Button, Card } from '../components/ui';

export function NotFoundPage() {
  return (
    <Card>
      <h1 className="text-lg font-semibold">Siden findes ikke</h1>
      <p className="mt-2 text-sm text-slate-600">
        Linket du fulgte er forkert eller forældet.
      </p>
      <div className="mt-4">
        <Link to="/">
          <Button>Tilbage til forsiden</Button>
        </Link>
      </div>
    </Card>
  );
}
