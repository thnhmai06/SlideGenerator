import { CheckmarkCircle01Icon, ExternalLinkIcon, Github01Icon } from "@/lib/icons";
import { createFileRoute } from "@tanstack/react-router";

const DEVELOPERS = [
  { initials: "MT", name: "Mai Thành", role: "Lead", color: "#7c3aed" },
  { initials: "HH", name: "Hoàng Huy", role: "Dev", color: "#1255b5" },
  { initials: "DK", name: "Đức Khải", role: "Dev", color: "#e08e0b" },
];

function AboutPage() {
  return (
    <div
      style={{
        flex: 1,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        padding: "64px 24px 40px",
        maxWidth: 720,
        width: "100%",
        margin: "0 auto",
        gap: 40,
      }}
    >
      {/* Logo + wordmark */}
      <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 16 }}>
        <img
          src="/app-icon.png"
          alt="SlideGenerator"
          style={{ width: 72, height: 72, borderRadius: 20 }}
        />
        <div style={{ textAlign: "center" }}>
          <h1
            style={{
              fontSize: "var(--f-2xl)",
              fontWeight: 900,
              letterSpacing: "-0.02em",
              color: "var(--tx)",
            }}
          >
            Slide Generator
          </h1>
          <p
            style={{
              fontSize: "var(--f-base)",
              fontWeight: 700,
              color: "var(--tx-mute)",
              marginTop: 6,
            }}
          >
            An automated, template-based presentation generator
          </p>
          <p
            style={{ fontSize: "var(--f-sm)", color: "var(--tx-dim)", marginTop: 4 }}
          >
            Tự động hoá tạo slide PowerPoint từ dữ liệu Excel và template
          </p>
        </div>
      </div>

      {/* Version card */}
      <div
        style={{
          width: "100%",
          background: "var(--surf)",
          border: "1px solid var(--bd)",
          borderRadius: "var(--r-lg)",
          padding: "var(--sp-6)",
        }}
      >
        <p
          style={{
            fontSize: "var(--f-2xs)",
            fontWeight: 800,
            letterSpacing: "0.08em",
            textTransform: "uppercase",
            color: "var(--tx-dim)",
            marginBottom: "var(--sp-4)",
          }}
        >
          PHIÊN BẢN &amp; CẬP NHẬT
        </p>

        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 12 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
            <span
              style={{
                fontSize: "var(--f-md)",
                fontWeight: 800,
                color: "var(--tx)",
              }}
            >
              SlideGenerator
            </span>
            <span
              style={{
                padding: "3px 10px",
                borderRadius: "var(--r-pill)",
                background: "var(--ac-soft)",
                color: "var(--ac)",
                fontSize: "var(--f-xs)",
                fontWeight: 700,
              }}
            >
              v1.0.0
            </span>
            <span style={{ display: "inline-flex", alignItems: "center", gap: 5 }}>
              <CheckmarkCircle01Icon size={14} style={{ color: "var(--su)" }} />
              <span style={{ fontSize: "var(--f-xs)", color: "var(--su)", fontWeight: 700 }}>
                Phiên bản mới nhất
              </span>
            </span>
          </div>
          <button
            type="button"
            style={{
              padding: "7px 14px",
              borderRadius: "var(--r-pill)",
              background: "var(--surf)",
              border: "1.5px solid var(--bd)",
              color: "var(--tx)",
              fontSize: "var(--f-sm)",
              fontWeight: 700,
              cursor: "pointer",
              transition: "border-color var(--tr-fast), color var(--tr-fast)",
            }}
            onMouseEnter={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--ac)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--ac)";
            }}
            onMouseLeave={(e) => {
              (e.currentTarget as HTMLButtonElement).style.borderColor = "var(--bd)";
              (e.currentTarget as HTMLButtonElement).style.color = "var(--tx)";
            }}
          >
            Kiểm tra cập nhật
          </button>
        </div>
      </div>

      {/* Developers */}
      <div style={{ width: "100%" }}>
        <p
          style={{
            fontSize: "var(--f-2xs)",
            fontWeight: 800,
            letterSpacing: "0.08em",
            textTransform: "uppercase",
            color: "var(--tx-dim)",
            marginBottom: "var(--sp-4)",
          }}
        >
          NHÀ PHÁT TRIỂN
        </p>
        <div style={{ display: "flex", gap: 24, justifyContent: "center", flexWrap: "wrap" }}>
          {DEVELOPERS.map((dev) => (
            <div
              key={dev.initials}
              style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 10 }}
            >
              <div style={{ position: "relative" }}>
                <div
                  style={{
                    width: 64,
                    height: 64,
                    borderRadius: "50%",
                    background: `${dev.color}22`,
                    border: `2px solid ${dev.color}44`,
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: "var(--f-md)",
                    fontWeight: 800,
                    color: dev.color,
                  }}
                >
                  {dev.initials}
                </div>
                <span
                  style={{
                    position: "absolute",
                    bottom: -4,
                    right: -4,
                    background: dev.color,
                    color: "#fff",
                    fontSize: 9,
                    fontWeight: 800,
                    padding: "2px 6px",
                    borderRadius: "var(--r-pill)",
                    letterSpacing: "0.04em",
                  }}
                >
                  {dev.role}
                </span>
              </div>
              <span style={{ fontSize: "var(--f-sm)", fontWeight: 700, color: "var(--tx)" }}>
                {dev.name}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* GitHub */}
      <a
        href="https://github.com/thnhmai06/SlideGenerator"
        target="_blank"
        rel="noreferrer"
        style={{
          display: "inline-flex",
          alignItems: "center",
          gap: 8,
          padding: "11px 24px",
          borderRadius: "var(--r-pill)",
          background: "var(--tx)",
          color: "var(--bg)",
          fontSize: "var(--f-base)",
          fontWeight: 700,
          textDecoration: "none",
          transition: "opacity var(--tr-fast)",
        }}
        onMouseEnter={(e) => { (e.currentTarget as HTMLAnchorElement).style.opacity = "0.85"; }}
        onMouseLeave={(e) => { (e.currentTarget as HTMLAnchorElement).style.opacity = "1"; }}
      >
        <Github01Icon size={18} />
        Kho lưu trữ GitHub
        <ExternalLinkIcon size={14} style={{ opacity: 0.7 }} />
      </a>
    </div>
  );
}

export const Route = createFileRoute("/about")({
  component: AboutPage,
});
