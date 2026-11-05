import { AbsoluteFill, Audio, interpolate, Sequence, staticFile, useCurrentFrame } from "remotion";
import { b, clamp, DURATION, FPS, MUSIC_START_SEC } from "./constants";
import { Background } from "./components/Background";
import { Intro } from "./scenes/Intro";
import { DemoApp, DemoCreate, DemoOpen } from "./scenes/Demo";
import { Features } from "./scenes/Features";
import { Montage } from "./scenes/Montage";
import { LogoOutro } from "./scenes/LogoOutro";
import { Flip, KineticScene } from "./scenes/Text";

// Music map (in beats of this video): groove 0-104, breakdown 104-124,
// drop 124-148, quiet outro after that.

// Scene changes get a soft background swell; only the big turns also get a flash.
const SCENES = [24, 48, 52, 64, 68, 96, 100, 104, 116, 124, 140];
const FLASHES = [24, 104, 124, 140];

const since = (f: number, beats: number[]) => {
  const past = beats.map(b).filter((x) => x <= f);
  return past.length ? f - past[past.length - 1] : Infinity;
};

const Scene: React.FC<{ from: number; to: number; children: React.ReactNode }> = ({ from, to, children }) => (
  <Sequence from={b(from)} durationInFrames={(to === Infinity ? DURATION : b(to)) - b(from)}>
    {children}
  </Sequence>
);

export const Promo: React.FC = () => {
  const f = useCurrentFrame();
  const pulse = Math.exp(-since(f, SCENES) / 12) * 0.8;
  const flash = interpolate(since(f, FLASHES), [0, 9], [0.35, 0], clamp);
  const fadeOut = interpolate(f, [DURATION - 24, DURATION - 2], [0, 1], clamp);

  return (
    <AbsoluteFill style={{ background: "#000" }}>
      <Audio
        src={staticFile("music.mp3")}
        startFrom={Math.round(MUSIC_START_SEC * FPS)}
        volume={(fr) => interpolate(fr, [0, 3, DURATION - 60, DURATION - 2], [0, 1, 1, 0], clamp)}
      />
      <Background pulse={pulse} />
      <AbsoluteFill>
        <Scene from={0} to={24}>
          <Intro />
        </Scene>
        <Scene from={24} to={48}>
          <DemoCreate />
        </Scene>
        <Scene from={48} to={52}>
          <KineticScene beat0={48} lines={[[{ t: "No more", beat: 48 }], [{ t: "lookalike", beat: 49 }, { t: "icons.", beat: 50, g: true }]]} />
        </Scene>
        <Scene from={52} to={64}>
          <DemoOpen />
        </Scene>
        <Scene from={64} to={68}>
          <KineticScene beat0={64} lines={[[{ t: "Want it", beat: 64, size: 140 }, { t: "as an", beat: 65, size: 140 }], [{ t: "app?", beat: 66, size: 300, g: true, v: "pop" }]]} />
        </Scene>
        <Scene from={68} to={96}>
          <DemoApp />
        </Scene>
        <Scene from={96} to={100}>
          <KineticScene
            beat0={96}
            lines={[
              [{ t: "Edge", beat: 96, size: 150, v: "rise" }, { t: "Chrome", beat: 96.5, size: 150, v: "rise" }],
              [{ t: "Brave", beat: 97, size: 150, v: "rise" }, { t: "Vivaldi", beat: 97.5, size: 150, v: "rise" }],
            ]}
          />
        </Scene>
        <Scene from={100} to={104}>
          <KineticScene beat0={100} lines={[[{ t: "Any browser.", beat: 100, size: 190, g: true }], [{ t: "Your choice.", beat: 101.5, size: 150, v: "rise" }]]} />
        </Scene>
        <Scene from={104} to={116}>
          <Features beat0={104} />
        </Scene>
        <Scene from={116} to={124}>
          <Flip
            beat0={116}
            endBeat={124}
            words={[
              { t: "Your links.", beat: 116, size: 170, v: "rise" },
              { t: "Your desktop.", beat: 118, size: 170, v: "rise", g: true },
              { t: "Ready?", beat: 120, size: 220 },
              { t: "3", beat: 121, size: 420 },
              { t: "2", beat: 122, size: 460 },
              { t: "1", beat: 123, size: 500, g: true },
            ]}
          />
        </Scene>
        <Scene from={124} to={140}>
          <Montage beat0={124} />
        </Scene>
        <Scene from={140} to={Infinity}>
          <LogoOutro beat0={140} />
        </Scene>
      </AbsoluteFill>
      <AbsoluteFill style={{ background: "#fff", opacity: flash, pointerEvents: "none" }} />
      <AbsoluteFill style={{ background: "#000", opacity: fadeOut }} />
    </AbsoluteFill>
  );
};
