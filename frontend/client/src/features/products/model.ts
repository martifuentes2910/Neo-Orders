export interface ProductFormValues {
  sku: string;
  name: string;
  category: string;
  basePrice: number;
  cost: number;
  stock: number;
  minStockAlert: number;
  active: boolean;
}

export const emptyProductForm: ProductFormValues = {
  sku: '',
  name: '',
  category: 'General',
  basePrice: 0,
  cost: 0,
  stock: 0,
  minStockAlert: 5,
  active: true,
};

export const IMAGE_MAX_BYTES = 2 * 1024 * 1024;
export const IMAGE_ACCEPT = 'image/jpeg,image/png,image/webp';

export function validateProductForm(form: ProductFormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!form.sku?.trim()) errors.sku = 'SKU obligatorio';
  if (!form.name?.trim()) errors.name = 'Nombre obligatorio';
  if (!form.category?.trim()) errors.category = 'Categoría obligatoria';
  if (!form.basePrice || form.basePrice <= 0) errors.basePrice = 'Precio inválido';
  return errors;
}

/** Devuelve el mensaje de error o `null` cuando la imagen es válida. */
export function validateImageFile(file: File): string | null {
  if (file.size > IMAGE_MAX_BYTES) return 'La imagen no puede superar los 2 MB.';
  if (!IMAGE_ACCEPT.split(',').includes(file.type)) return 'Formato no admitido. Use JPG, PNG o WebP.';
  return null;
}
