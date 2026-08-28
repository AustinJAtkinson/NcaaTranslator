export type TabItem = {
  id: string;
  label: string;
};

export default function Tabs({
  items,
  value,
  onChange,
  nested = false,
  ariaLabel,
}: {
  items: TabItem[];
  value: string;
  onChange: (id: string) => void;
  nested?: boolean;
  ariaLabel: string;
}) {
  return (
    <div className={nested ? "tab-strip nested" : "tab-strip"} role="tablist" aria-label={ariaLabel}>
      {items.map((item) => {
        const selected = item.id === value;
        return (
          <button
            key={item.id}
            type="button"
            role="tab"
            aria-selected={selected}
            className={selected ? "tab-item active" : "tab-item"}
            onClick={() => onChange(item.id)}
          >
            {item.label}
          </button>
        );
      })}
    </div>
  );
}
