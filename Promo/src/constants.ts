import { useVideoConfig } from "remotion";

export const FPS = 30;
export const BPM = 136;
// Frames per beat at 136 BPM.
export const BEAT = (FPS * 60) / BPM;
// Where in "Fresh Energy.mp3" the promo starts: a downbeat chosen so the track's
// drop (85.63s) lands on beat 124 of the video.
export const MUSIC_START_SEC = 30.922;
export const DURATION = 2100;

export const b = (n: number) => Math.round(n * BEAT);
export const bar = (n: number) => b(n * 4);

// The screen recording.
export const SRC_W = 1196;
export const SRC_H = 720;

export const COLORS = {
  blue: "#2266DD",
  teal: "#1AAAB0",
  navy: "#061536",
  ink: "#030B1F",
  cyan: "#4FE3FF",
  accent: "#FFD23F",
};

export const GRADIENT = `linear-gradient(135deg, ${COLORS.blue} 0%, ${COLORS.teal} 100%)`;
export const TEXT_GRADIENT = `linear-gradient(90deg, ${COLORS.cyan} 0%, #9BF5C9 50%, ${COLORS.accent} 100%)`;
export const FONT = '"Segoe UI Variable Display", "Segoe UI", system-ui, sans-serif';

// Layout helper: `u` scales sizes authored for 1920x1080 to the vertical frame.
export const useLayout = () => {
  const { width, height } = useVideoConfig();
  const tall = height > width;
  return { tall, W: width, H: height, u: tall ? 0.74 : 1 };
};

export const clamp = { extrapolateLeft: "clamp", extrapolateRight: "clamp" } as const;
