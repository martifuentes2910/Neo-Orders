/** Deriva una miniatura de Cloudinary sin descargar la imagen completa. Si no es una URL de Cloudinary, devuelve la original. */
export function thumbnailUrl(url?: string | null): string | null {
  if (!url) return null;
  if (!url.includes('/image/upload/')) return url;
  return url.replace('/image/upload/', '/image/upload/c_fill,g_auto,w_96,h_96,q_auto,f_auto/');
}
