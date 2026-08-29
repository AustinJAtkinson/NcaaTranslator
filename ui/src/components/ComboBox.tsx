import { useEffect, useId, useLayoutEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";
import { createPortal } from "react-dom";
import { ChevronDownIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export type ComboOption = {
  display: string;
  value: string;
};

let suppressOpen = false;

function beginSuppressOpen(): void {
  if (suppressOpen) return;
  suppressOpen = true;
  const end = () => {
    suppressOpen = false;
    document.removeEventListener("pointerup", end, true);
    document.removeEventListener("pointercancel", end, true);
  };
  document.addEventListener("pointerup", end, true);
  document.addEventListener("pointercancel", end, true);
}

export default function ComboBox({
  value,
  options,
  editable = true,
  filterOnType = false,
  width,
  onSelect,
  onBlurText,
  onSelectedValueChange,
}: {
  value: string | null;
  options: ComboOption[];
  editable?: boolean;
  filterOnType?: boolean;
  width?: number;
  onSelect: (value: string) => void;
  onBlurText?: (text: string) => void;
  onSelectedValueChange?: (value: string | null) => void;
}) {
  const [open, setOpen] = useState(false);
  const [text, setText] = useState(() => displayFor(value, options));
  const [highlight, setHighlight] = useState(0);
  const [menuPos, setMenuPos] = useState<{ top: number; left: number; width: number } | null>(null);
  const skipBlur = useRef(false);
  const rootRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const listId = useId();

  useEffect(() => {
    setText(displayFor(value, options));
  }, [value, options]);

  useLayoutEffect(() => {
    if (!open) {
      setMenuPos(null);
      return;
    }
    function updatePos(): void {
      const el = rootRef.current;
      if (!el) return;
      const rect = el.getBoundingClientRect();
      setMenuPos({ top: rect.bottom + 4, left: rect.left, width: rect.width });
    }
    updatePos();
    window.addEventListener("resize", updatePos);
    window.addEventListener("scroll", updatePos, true);
    return () => {
      window.removeEventListener("resize", updatePos);
      window.removeEventListener("scroll", updatePos, true);
    };
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function onPointerDown(event: Event): void {
      const target = event.target as Node | null;
      if (!target) return;
      if (rootRef.current?.contains(target)) return;
      const option = (event.target as HTMLElement | null)?.closest?.("[role='option']");
      if (option && listRef.current?.contains(option)) return;
      beginSuppressOpen();
      setOpen(false);
    }
    document.addEventListener("pointerdown", onPointerDown, true);
    return () => document.removeEventListener("pointerdown", onPointerDown, true);
  }, [open]);

  const visible = useMemo(() => {
    if (!filterOnType) return options;
    const q = text.trim().toLowerCase();
    if (!q) return options;
    return options.filter(
      (option) => option.display.toLowerCase().includes(q) || option.value.toLowerCase().includes(q)
    );
  }, [filterOnType, options, text]);

  function close(): void {
    setOpen(false);
  }

  function pick(option: ComboOption): void {
    skipBlur.current = true;
    setText(option.display);
    close();
    onSelectedValueChange?.(option.value);
    onSelect(option.value);
  }

  function handleInput(next: string): void {
    setText(next);
    setOpen(true);
    const match = options.find((option) => option.display === next || option.value === next);
    onSelectedValueChange?.(match?.value ?? null);
  }

  function handleBlur(): void {
    if (skipBlur.current) {
      skipBlur.current = false;
      return;
    }
    close();
    onBlurText?.(text);
  }

  function requestOpen(): void {
    if (suppressOpen) return;
    setOpen(true);
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>): void {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      if (!open) {
        setOpen(true);
        setHighlight(0);
        return;
      }
      setHighlight((index) => Math.min(index + 1, Math.max(visible.length - 1, 0)));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setHighlight((index) => Math.max(index - 1, 0));
    } else if (event.key === "Enter") {
      if (open && visible[highlight]) {
        event.preventDefault();
        pick(visible[highlight]);
      }
    } else if (event.key === "Escape") {
      close();
    }
  }

  const menu =
    open && menuPos ? (
      <ul
        ref={listRef}
        id={listId}
        data-combo-menu
        role="listbox"
        style={{ top: menuPos.top, left: menuPos.left, width: menuPos.width }}
        className="fixed z-50 max-h-60 min-w-full overflow-auto rounded-lg border border-border bg-popover p-1 text-popover-foreground shadow-md"
      >
        {visible.length === 0 ? (
          <li className="cursor-default px-2 py-1.5 text-sm text-muted-foreground" aria-disabled="true">
            No matches
          </li>
        ) : (
          visible.map((option, index) => (
            <li
              id={`${listId}-${index}`}
              key={`${option.value}-${index}`}
              role="option"
              aria-selected={index === highlight}
              className={cn(
                "cursor-pointer rounded-sm px-2 py-1.5 text-sm text-foreground",
                index === highlight && "bg-accent/15",
              )}
              onMouseEnter={() => setHighlight(index)}
              onMouseDown={(event) => {
                event.preventDefault();
                pick(option);
              }}
            >
              {option.display}
            </li>
          ))
        )}
      </ul>
    ) : null;

  return (
    <div
      ref={rootRef}
      className="relative inline-flex h-8 items-stretch rounded-[6px] border border-input bg-card text-sm shadow-xs focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/50"
      style={width ? { width } : undefined}
    >
      <input
        className="h-full min-w-0 flex-1 rounded-l-[6px] border-0 bg-transparent px-2 outline-none"
        role="combobox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete={filterOnType ? "list" : "none"}
        aria-activedescendant={open && visible[highlight] ? `${listId}-${highlight}` : undefined}
        value={text}
        readOnly={!editable}
        onChange={(event) => handleInput(event.target.value)}
        onFocus={requestOpen}
        onBlur={handleBlur}
        onKeyDown={onKeyDown}
      />
      <button
        type="button"
        className="flex w-6 shrink-0 items-center justify-center text-muted-foreground"
        tabIndex={-1}
        aria-label="Open"
        onMouseDown={(event) => {
          event.preventDefault();
          if (suppressOpen) return;
          setOpen((was) => !was);
        }}
      >
        <ChevronDownIcon className="size-4" aria-hidden="true" />
      </button>
      {menu ? createPortal(menu, document.body) : null}
    </div>
  );
}

function displayFor(value: string | null, options: ComboOption[]): string {
  if (value == null || value === "") return "";
  return options.find((option) => option.value === value)?.display ?? value;
}
