/** Cantidad de avatares prediseñados disponibles (claves avatar-1 … avatar-N). */
export const AVATAR_COUNT = 12;

/** Claves estables para el selector y los formularios. */
export const AVATAR_KEYS = Array.from({ length: AVATAR_COUNT }, (_, i) => `avatar-${i + 1}`);
