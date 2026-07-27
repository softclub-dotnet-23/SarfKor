// The Shape Detection API's BarcodeDetector isn't part of TypeScript's DOM lib yet — supported
// natively in Chromium browsers (desktop + Android), absent in Firefox/Safari. Feature-detected
// at runtime via `'BarcodeDetector' in window` before any of this is touched.
interface DetectedBarcode {
  rawValue: string
  format: string
}

declare class BarcodeDetector {
  constructor(options?: { formats?: string[] })
  detect(source: CanvasImageSource): Promise<DetectedBarcode[]>
  static getSupportedFormats(): Promise<string[]>
}
