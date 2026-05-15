import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';

export function NotFoundPage() {
  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <h1 className="font-serif text-2xl tracking-tight">Siden findes ikke</h1>
        <p className="text-muted-foreground text-sm">
          Linket du fulgte er forkert eller forældet.
        </p>
        <div className="pt-2">
          <Button asChild>
            <Link to="/">Tilbage til forsiden</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
