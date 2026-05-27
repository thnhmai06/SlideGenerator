import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { motion } from "motion/react";
import { useEffect, useState } from "react";

function SplashPage() {
  const navigate = useNavigate();
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => {
      setProgress((p) => {
        if (p >= 100) {
          clearInterval(timer);
          return 100;
        }
        return p + 3;
      });
    }, 36);

    const navTimer = setTimeout(() => {
      navigate({ to: "/recipes" });
    }, 1400);

    return () => {
      clearInterval(timer);
      clearTimeout(navTimer);
    };
  }, [navigate]);

  return (
    <div
      style={{
        minHeight: "100vh",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        background: "var(--bg)",
        gap: 32,
      }}
    >
      <motion.div
        initial={{ opacity: 0, y: 12 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.35, ease: [0.165, 0.84, 0.44, 1] }}
        style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 24 }}
      >
        {/* Logo + wordmark row */}
        <div style={{ display: "flex", alignItems: "center", gap: 20 }}>
          <motion.img
            src="/app-icon.png"
            alt="SlideGenerator"
            style={{ width: 64, height: 64, borderRadius: 16 }}
            initial={{ scale: 0.82 }}
            animate={{ scale: 1 }}
            transition={{ duration: 0.45, ease: [0.165, 0.84, 0.44, 1] }}
          />
          <h1
            style={{
              fontSize: 42,
              fontWeight: 900,
              letterSpacing: "-0.03em",
              lineHeight: 1,
              background: "linear-gradient(135deg, var(--ac) 0%, var(--ac-hover) 100%)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            Slide Generator
          </h1>
        </div>

        {/* Progress bar */}
        <div style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 10 }}>
          <div
            style={{
              width: 320,
              height: 3,
              background: "var(--bg-soft)",
              borderRadius: "var(--r-pill)",
              overflow: "hidden",
            }}
          >
            <motion.div
              style={{
                height: "100%",
                background: "linear-gradient(90deg, var(--ac), var(--ac-hover))",
                borderRadius: "var(--r-pill)",
                width: `${progress}%`,
              }}
              transition={{ duration: 0.05 }}
            />
          </div>

          <span
            style={{
              fontSize: "var(--f-2xs)",
              fontWeight: 700,
              letterSpacing: "0.12em",
              textTransform: "uppercase",
              color: "var(--tx-dim)",
            }}
          >
            KHỞI TẠO ỨNG DỤNG...
          </span>
        </div>
      </motion.div>
    </div>
  );
}

export const Route = createFileRoute("/splash")({
  component: SplashPage,
});
