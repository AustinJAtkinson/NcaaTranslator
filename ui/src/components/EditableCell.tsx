import { useEffect, useRef, useState, type KeyboardEvent, type RefObject } from "react";
import type { ComboOption } from "./ComboBox";

export default function EditableCell({
  value,
  readOnly = false,
  options,
  onCommit,
}: {
  value: string;
  readOnly?: boolean;
  options?: ComboOption[];
  onCommit: (value: string) => void;
}) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);
  const inputRef = useRef<HTMLInputElement | HTMLSelectElement>(null);

  useEffect(() => {
    setDraft(value);
  }, [value]);

  useEffect(() => {
    if (editing) inputRef.current?.focus();
  }, [editing]);

  function beginEdit(): void {
    if (readOnly) return;
    setDraft(value);
    setEditing(true);
  }

  function commit(): void {
    setEditing(false);
    if (draft !== value) onCommit(draft);
  }

  function cancel(): void {
    setDraft(value);
    setEditing(false);
  }

  function onKeyDown(event: KeyboardEvent): void {
    if (event.key === "Enter") {
      event.preventDefault();
      commit();
    } else if (event.key === "Escape") {
      event.preventDefault();
      cancel();
    } else if (!editing && (event.key === "F2" || event.key === "Enter")) {
      event.preventDefault();
      beginEdit();
    }
  }

  if (!editing) {
    const shown = options?.find((option) => option.value === value)?.display ?? value;
    return (
      <td
        className={readOnly ? "cell" : "cell editable"}
        tabIndex={readOnly ? undefined : 0}
        onDoubleClick={beginEdit}
        onKeyDown={onKeyDown}
      >
        {shown}
      </td>
    );
  }

  if (options) {
    const hasCurrent = options.some((option) => option.value === draft);
    return (
      <td className="cell editing">
        <select
          ref={inputRef as RefObject<HTMLSelectElement>}
          value={hasCurrent ? draft : ""}
          onChange={(event) => {
            setDraft(event.target.value);
            onCommit(event.target.value);
            setEditing(false);
          }}
          onBlur={commit}
          onKeyDown={onKeyDown}
        >
          {!hasCurrent && <option value={draft}>{draft}</option>}
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.display}
            </option>
          ))}
        </select>
      </td>
    );
  }

  return (
    <td className="cell editing">
      <input
        ref={inputRef as RefObject<HTMLInputElement>}
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onBlur={commit}
        onKeyDown={onKeyDown}
      />
    </td>
  );
}
