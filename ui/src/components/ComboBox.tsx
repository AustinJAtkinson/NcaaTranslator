import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from "react";

export type ComboOption = {
  display: string;
  value: string;
};

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
  const skipBlur = useRef(false);

  useEffect(() => {
    setText(displayFor(value, options));
  }, [value, options]);

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

  return (
    <div
      className="combo"
      style={width ? { width } : undefined}
      onMouseDown={(event) => {
        if ((event.target as HTMLElement).closest(".combo-menu")) skipBlur.current = true;
      }}
    >
      <input
        className="combo-input"
        value={text}
        readOnly={!editable}
        onChange={(event) => handleInput(event.target.value)}
        onFocus={() => setOpen(true)}
        onBlur={handleBlur}
        onKeyDown={onKeyDown}
      />
      <button
        type="button"
        className="combo-chevron"
        tabIndex={-1}
        aria-label="Open"
        onMouseDown={(event) => {
          event.preventDefault();
          setOpen((was) => !was);
        }}
      >
        <svg width="8" height="5" viewBox="0 0 8 5" aria-hidden="true">
          <path d="M0 0 L4 4 L8 0" fill="none" stroke="currentColor" strokeWidth="2" />
        </svg>
      </button>
      {open && (
        <ul className="combo-menu">
          {visible.map((option, index) => (
            <li
              key={`${option.value}-${index}`}
              className={index === highlight ? "combo-item highlight" : "combo-item"}
              onMouseEnter={() => setHighlight(index)}
              onMouseDown={(event) => {
                event.preventDefault();
                pick(option);
              }}
            >
              {option.display}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function displayFor(value: string | null, options: ComboOption[]): string {
  if (value == null || value === "") return "";
  return options.find((option) => option.value === value)?.display ?? value;
}
