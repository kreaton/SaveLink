import { ScreenCard } from "../components/ScreenCard";

// UI rectangles in the recording (1196x720), measured from frames.
export const UI = {
  addressBar: [650, 40, 925, 68],
  urlField: [172, 86, 554, 106],
  iconPreview: [172, 166, 232, 224],
  createButton: [293, 399, 435, 423],
  createdText: [294, 436, 434, 453],
  appCheckbox: [194, 352, 480, 371],
  desktopIcon1: [4, 258, 66, 318],
  desktopIcon2: [4, 343, 66, 403],
  browserWindow: [588, 4, 1162, 488],
  appTitleBar: [15, 6, 800, 36],
} satisfies Record<string, [number, number, number, number]>;

// Beats 24-48: drag a link in, click Create, shortcut appears.
export const DemoCreate: React.FC = () => (
  <ScreenCard
    srcStart={10.6}
    tags={[
      { t: 10.6, num: "01", text: "Drag in any link" },
      { t: 15.9, num: "02", text: "Click Create" },
    ]}
    keys={[
      { t: 10.6, cx: 598, cy: 360, z: 1, tcx: 380 },
      { t: 11.4, cx: 660, cy: 230, z: 1.6, dur: 18, tcx: 660 },
      { t: 12.0, cx: 720, cy: 70, z: 2.6, dur: 14 },
      { t: 13.0, cx: 470, cy: 100, z: 2.1, dur: 24 },
      { t: 14.5, cx: 330, cy: 165, z: 2.7, dur: 14 },
      { t: 16.1, cx: 364, cy: 405, z: 2.8, dur: 14 },
      { t: 18.6, cx: 60, cy: 300, z: 3.2, dur: 12, tcx: 120 },
    ]}
    callouts={[
      { from: 12.15, to: 13.3, rect: UI.addressBar, label: "Grab any link" },
      { from: 14.6, to: 15.85, rect: UI.iconPreview, label: "Finds the site's real icon" },
      { from: 16.5, to: 17.4, rect: UI.createButton, label: "One click" },
      { from: 17.5, to: 18.55, rect: UI.createdText, label: "Shortcut created" },
      { from: 19.05, to: 21.3, rect: UI.desktopIcon1, label: "On your desktop, with its icon" },
    ]}
    clicks={[{ t: 17.2, x: 364, y: 412 }]}
  />
);

// Beats 52-64: double-click the new shortcut, it opens in the browser.
export const DemoOpen: React.FC = () => (
  <ScreenCard
    srcStart={25.9}
    rate={1.5}
    tags={[{ t: 25.9, num: "03", text: "Double-click to open" }]}
    keys={[
      { t: 25.9, cx: 60, cy: 300, z: 3.0, tcx: 120 },
      { t: 28.4, cx: 640, cy: 260, z: 1.2, dur: 22, tcx: 870, tz: 2.2 },
    ]}
    callouts={[
      { from: 26.1, to: 28.3, rect: UI.desktopIcon1, label: "Double-click" },
      { from: 29.9, to: 33.9, rect: UI.browserWindow, label: "Opens in your browser" },
    ]}
    clicks={[
      { t: 26.6, x: 35, y: 285 },
      { t: 26.85, x: 35, y: 285 },
    ]}
  />
);

// Beats 68-96: app-window mode and the result.
export const DemoApp: React.FC = () => (
  <ScreenCard
    srcStart={35.9}
    rate={0.92}
    tags={[{ t: 35.9, num: "04", text: "Or give it its own window" }]}
    keys={[
      { t: 35.9, cx: 360, cy: 300, z: 1.6 },
      { t: 36.3, cx: 330, cy: 365, z: 2.8, dur: 14 },
      { t: 37.8, cx: 364, cy: 410, z: 2.7, dur: 10 },
      { t: 39.4, cx: 60, cy: 370, z: 3.1, dur: 12, tcx: 120 },
      { t: 41.2, cx: 420, cy: 360, z: 1.05, dur: 16, tcx: 400 },
      { t: 42.3, cx: 300, cy: 40, z: 2.0, dur: 18, tcx: 260 },
      { t: 44.4, cx: 410, cy: 330, z: 1.2, dur: 24, tcx: 400 },
    ]}
    callouts={[
      { from: 36.8, to: 37.85, rect: UI.appCheckbox, label: "Open in its own app window" },
      { from: 38.4, to: 39.35, rect: UI.createButton, label: "Create" },
      { from: 39.95, to: 41.15, rect: UI.desktopIcon2, label: "App shortcut" },
      { from: 42.8, to: 44.4, rect: UI.appTitleBar, label: "No tabs. No address bar." },
      { from: 45.0, to: 47.25, rect: [15, 6, 800, 716], label: "Feels like a real app" },
    ]}
    clicks={[
      { t: 37.05, x: 200, y: 361 },
      { t: 38.3, x: 364, y: 412 },
      { t: 40.6, x: 35, y: 372 },
      { t: 40.85, x: 35, y: 372 },
    ]}
  />
);
