import { useState } from "react";
import {
  Languages,
  LayoutList,
  Moon,
  PanelLeftClose,
  PanelLeftOpen,
  Settings,
  Sun,
  type LucideIcon,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { getTheme, setTheme, type ThemeName } from "../theme";

export type SectionId = "main" | "settings" | "names";
export type SettingsSubId = "general" | "sports" | "display-teams" | "xml";
export type NamesSubId = "teams" | "conferences";
export type NavId =
  | "main"
  | "settings"
  | "settings-general"
  | "settings-sports"
  | "settings-display"
  | "settings-xml"
  | "names"
  | "names-teams"
  | "names-conferences";

const COLLAPSE_KEY = "ncaa-sidebar-collapsed";

type NavChild = { id: NavId; label: string };
type NavItem = {
  id: NavId;
  label: string;
  icon: LucideIcon;
  children?: NavChild[];
};

const NAV_ITEMS: NavItem[] = [
  { id: "main", label: "Scoreboard", icon: LayoutList },
  {
    id: "settings",
    label: "Settings",
    icon: Settings,
    children: [
      { id: "settings-general", label: "General" },
      { id: "settings-sports", label: "Sports" },
      { id: "settings-display", label: "Display" },
      { id: "settings-xml", label: "XML" },
    ],
  },
  {
    id: "names",
    label: "Names",
    icon: Languages,
    children: [
      { id: "names-teams", label: "Teams" },
      { id: "names-conferences", label: "Conferences" },
    ],
  },
];

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSE_KEY) === "true";
  } catch {
    return false;
  }
}

function writeCollapsed(collapsed: boolean): void {
  try {
    localStorage.setItem(COLLAPSE_KEY, collapsed ? "true" : "false");
  } catch {
    /* quota / privacy */
  }
}

function sectionOf(id: NavId): SectionId {
  if (id === "main") return "main";
  if (id === "names" || id.startsWith("names-")) return "names";
  return "settings";
}

export default function Sidebar({
  activeSection,
  settingsSub = "general",
  namesSub = "teams",
  onNavigate,
}: {
  activeSection: SectionId;
  settingsSub?: SettingsSubId;
  namesSub?: NamesSubId;
  onNavigate: (id: NavId) => void;
}) {
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const [theme, setThemeState] = useState<ThemeName>(getTheme);

  function toggleCollapsed(): void {
    setCollapsed((prev) => {
      const next = !prev;
      writeCollapsed(next);
      return next;
    });
  }

  function toggleTheme(): void {
    const next: ThemeName = theme === "dark" ? "light" : "dark";
    setTheme(next);
    setThemeState(next);
  }

  function isChildActive(id: NavId): boolean {
    if (activeSection === "settings") {
      if (id === "settings-general") return settingsSub === "general";
      if (id === "settings-sports") return settingsSub === "sports";
      if (id === "settings-display") return settingsSub === "display-teams";
      if (id === "settings-xml") return settingsSub === "xml";
    }
    if (activeSection === "names") {
      if (id === "names-teams") return namesSub === "teams";
      if (id === "names-conferences") return namesSub === "conferences";
    }
    return false;
  }

  return (
    <aside
      className={cn(
        "flex h-full shrink-0 flex-col overflow-hidden border-r border-border bg-card",
        collapsed ? "w-[52px]" : "w-[200px]",
      )}
      data-collapsed={collapsed ? "true" : "false"}
      aria-label="Sidebar"
    >
      <div
        className={cn(
          "flex h-11 shrink-0 items-center",
          collapsed ? "justify-center px-1" : "px-3",
        )}
      >
        {collapsed ? (
          <span className="text-[13px] font-semibold text-foreground" aria-hidden>
            N
          </span>
        ) : (
          <div className="min-w-0">
            <div className="text-[13px] font-semibold leading-tight text-foreground">
              NCAA Translator
            </div>
          </div>
        )}
      </div>

      <nav className="flex min-h-0 flex-1 flex-col gap-0.5 overflow-y-auto px-2" aria-label="App sections">
        {NAV_ITEMS.map((item) => {
          const Icon = item.icon;
          const active = activeSection === sectionOf(item.id);
          return (
            <div key={item.id} className="flex flex-col gap-0.5">
              <button
                type="button"
                aria-current={
                  collapsed && active ? "page" : active && !item.children ? "page" : undefined
                }
                aria-expanded={item.children ? !collapsed : undefined}
                aria-label={item.label}
                title={collapsed ? item.label : undefined}
                className={cn(
                  "relative flex cursor-pointer items-center rounded-[6px] text-[13px] outline-none",
                  "hover:bg-muted focus-visible:ring-[3px] focus-visible:ring-ring/50",
                  collapsed ? "h-8 w-8 justify-center self-center px-0" : "h-8 gap-2 px-2",
                  active && "bg-primary/10 text-primary",
                  active &&
                    "before:absolute before:top-1/2 before:left-0 before:h-4 before:w-[2px] before:-translate-y-1/2 before:rounded-full before:bg-primary",
                )}
                onClick={() => onNavigate(item.id)}
              >
                <Icon className={cn("size-4 shrink-0", active ? "text-primary" : "text-muted-foreground")} />
                {!collapsed && <span className="truncate font-medium">{item.label}</span>}
              </button>
              {!collapsed &&
                item.children?.map((child) => {
                  const childActive = isChildActive(child.id);
                  return (
                    <button
                      key={child.id}
                      type="button"
                      aria-current={childActive ? "page" : undefined}
                      aria-label={child.label}
                      className={cn(
                        "relative flex h-7 cursor-pointer items-center rounded-[6px] pl-[20px] pr-2 text-[13px] outline-none",
                        "text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:ring-[3px] focus-visible:ring-ring/50",
                        childActive && "bg-primary/10 font-medium text-primary",
                      )}
                      onClick={() => onNavigate(child.id)}
                    >
                      <span className="truncate">{child.label}</span>
                    </button>
                  );
                })}
            </div>
          );
        })}
      </nav>

      <div
        className={cn(
          "mt-auto flex shrink-0 gap-1 border-t border-border p-2",
          collapsed ? "flex-col items-center" : "flex-row items-center justify-between",
        )}
      >
        <button
          type="button"
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          title={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          className="flex size-8 cursor-pointer items-center justify-center rounded-[6px] text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/60"
          onClick={toggleCollapsed}
        >
          {collapsed ? <PanelLeftOpen className="size-4" /> : <PanelLeftClose className="size-4" />}
        </button>
        <button
          type="button"
          aria-label={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
          title={theme === "dark" ? "Switch to light theme" : "Switch to dark theme"}
          className="flex size-8 cursor-pointer items-center justify-center rounded-[6px] text-muted-foreground outline-none hover:bg-muted hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring/60"
          onClick={toggleTheme}
        >
          {theme === "dark" ? <Sun className="size-4" /> : <Moon className="size-4" />}
        </button>
      </div>
    </aside>
  );
}
