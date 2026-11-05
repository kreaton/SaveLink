import { Sequence } from "remotion";
import { b } from "../constants";
import { Kinetic, Line, Word } from "../components/Kinetic";

export type W = { t: string; beat: number; size?: number; g?: boolean; v?: "slam" | "rise" | "pop"; color?: string };

// Frames from the scene's first beat to `beat` (both global beat numbers), so
// words stay on the global beat grid despite rounding.
export const rel = (beat0: number, beat: number) => b(beat) - b(beat0);

// Lines of words, each landing on its own beat.
export const KineticScene: React.FC<{ beat0: number; lines: W[][]; style?: React.CSSProperties }> = ({ beat0, lines, style }) => (
  <Kinetic style={style}>
    {lines.map((line, i) => (
      <Line key={i}>
        {line.map((w) => (
          <Word key={w.t + w.beat} at={rel(beat0, w.beat)} size={w.size ?? 150} gradient={w.g} variant={w.v} color={w.color}>
            {w.t}
          </Word>
        ))}
      </Line>
    ))}
  </Kinetic>
);

// One word at a time, each replacing the last on its beat.
export const Flip: React.FC<{ beat0: number; words: W[]; endBeat: number }> = ({ beat0, words, endBeat }) => (
  <>
    {words.map((w, i) => {
      const from = rel(beat0, w.beat);
      const to = rel(beat0, i + 1 < words.length ? words[i + 1].beat : endBeat);
      return (
        <Sequence key={i} from={from} durationInFrames={to - from} layout="none">
          <KineticScene beat0={w.beat} lines={[[{ ...w }]]} />
        </Sequence>
      );
    })}
  </>
);
