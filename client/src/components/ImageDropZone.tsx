import { useRef, useState, type DragEvent } from 'react';
import { ImagePlus, Loader2, RefreshCw, Trash2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

const MAX_BYTES = 8 * 1024 * 1024;
const ACCEPTED = ['image/jpeg', 'image/png', 'image/webp'];

interface ImageDropZoneProps {
  imageUrl: string | null;
  onPick: (file: File) => void;
  onRemove: () => void;
  busy?: boolean;
  disabled?: boolean;
}

export function ImageDropZone({
  imageUrl,
  onPick,
  onRemove,
  busy = false,
  disabled = false,
}: ImageDropZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const locked = disabled || busy;

  function validateAndPick(file: File | undefined) {
    if (!file) return;
    if (!ACCEPTED.includes(file.type)) {
      toast.error('Billedet skal være JPEG, PNG eller WebP.');
      return;
    }
    if (file.size > MAX_BYTES) {
      toast.error('Billedet er for stort. Maks 8 MB.');
      return;
    }
    onPick(file);
  }

  function openPicker() {
    if (locked) return;
    inputRef.current?.click();
  }

  function handleDrop(e: DragEvent) {
    e.preventDefault();
    setDragging(false);
    if (locked) return;
    validateAndPick(e.dataTransfer.files?.[0]);
  }

  return (
    <div>
      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED.join(',')}
        className="sr-only"
        onChange={(e) => {
          validateAndPick(e.target.files?.[0]);
          e.target.value = '';
        }}
      />

      {imageUrl ? (
        <div className="space-y-2">
          <div className="relative overflow-hidden rounded-lg border">
            <img src={imageUrl} alt="" className="aspect-[3/2] w-full object-cover" />
            {busy && (
              <div className="absolute inset-0 flex items-center justify-center bg-black/40">
                <Loader2 className="size-6 animate-spin text-white" />
              </div>
            )}
          </div>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={openPicker}
              disabled={locked}
            >
              <RefreshCw className="size-4" /> Erstat
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={onRemove}
              disabled={locked}
              className="text-muted-foreground hover:text-destructive"
            >
              <Trash2 className="size-4" /> Fjern
            </Button>
          </div>
        </div>
      ) : (
        <button
          type="button"
          onClick={openPicker}
          onDragOver={(e) => {
            e.preventDefault();
            if (!locked) setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={handleDrop}
          disabled={locked}
          className={cn(
            'flex aspect-[3/2] w-full flex-col items-center justify-center gap-2 rounded-lg border border-dashed px-4 text-center transition',
            dragging
              ? 'border-primary bg-primary/5'
              : 'border-border bg-muted/30 hover:bg-muted/50',
            locked && 'pointer-events-none opacity-60',
          )}
        >
          {busy ? (
            <Loader2 className="size-6 animate-spin" />
          ) : (
            <ImagePlus className="text-muted-foreground size-7" />
          )}
          <div className="space-y-0.5">
            <div className="text-sm font-medium">Træk billede hertil eller klik for at vælge</div>
            <div className="text-muted-foreground text-xs">
              JPEG, PNG eller WebP · Maks 8 MB · Brede billeder (3:2) ser bedst ud
            </div>
          </div>
        </button>
      )}
    </div>
  );
}
