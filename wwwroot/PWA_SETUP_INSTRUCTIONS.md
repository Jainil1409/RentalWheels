# PWA Icon Generation Instructions

## You need to create app icons in the following sizes:

1. Create a folder: `wwwroot/images/icons/`

2. Generate icons from your logo in these sizes:
   - icon-72x72.png
   - icon-96x96.png
   - icon-128x128.png
   - icon-144x144.png
   - icon-152x152.png
   - icon-192x192.png
   - icon-384x384.png
   - icon-512x512.png

## Quick Way to Generate Icons:

### Option 1: Use Online Tool (Easiest)
1. Go to: https://www.pwabuilder.com/imageGenerator
2. Upload your logo.png
3. Download the generated icons
4. Extract and place in `wwwroot/images/icons/`

### Option 2: Use Photoshop/GIMP
1. Open your logo.png
2. Resize to each size listed above
3. Export as PNG
4. Save in `wwwroot/images/icons/`

### Option 3: Use ImageMagick (Command Line)
```bash
# Install ImageMagick first
# Then run these commands:

convert logo.png -resize 72x72 icon-72x72.png
convert logo.png -resize 96x96 icon-96x96.png
convert logo.png -resize 128x128 icon-128x128.png
convert logo.png -resize 144x144 icon-144x144.png
convert logo.png -resize 152x152 icon-152x152.png
convert logo.png -resize 192x192 icon-192x192.png
convert logo.png -resize 384x384 icon-384x384.png
convert logo.png -resize 512x512 icon-512x512.png
```

## Screenshots (Optional but Recommended)
Create folder: `wwwroot/images/screenshots/`
Take screenshots of your app:
- screenshot1.png (540x720)
- screenshot2.png (540x720)

These will appear in the app store listing when users install your PWA.

## Testing Your PWA

1. Run your application
2. Open Chrome DevTools (F12)
3. Go to "Application" tab
4. Check:
   - Manifest
   - Service Workers
   - Storage
5. Use Lighthouse to audit your PWA

## PWA Checklist
✅ manifest.json created
✅ Service worker registered
✅ Icons generated (YOU NEED TO DO THIS)
✅ HTTPS enabled (required for PWA)
✅ Responsive design
✅ Offline page created
✅ Install prompt added
