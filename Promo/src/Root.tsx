import { Composition } from "remotion";
import { DURATION, FPS } from "./constants";
import { Promo } from "./Promo";
import { ContactSheet } from "./ContactSheet";
import { GridFrame } from "./GridFrame";

export const RemotionRoot: React.FC = () => (
  <>
    <Composition id="SaveLinkPromo" component={Promo} width={1920} height={1080} fps={FPS} durationInFrames={DURATION} />
    <Composition id="SaveLinkPromoVertical" component={Promo} width={1080} height={1920} fps={FPS} durationInFrames={DURATION} />
    <Composition id="ContactSheet" component={ContactSheet} width={1920} height={1156} fps={30} durationInFrames={1} defaultProps={{ start: 0, step: 1.75 }} />
    <Composition id="GridFrame" component={GridFrame} width={1196} height={720} fps={30} durationInFrames={1} defaultProps={{ t: 0 }} />
  </>
);
