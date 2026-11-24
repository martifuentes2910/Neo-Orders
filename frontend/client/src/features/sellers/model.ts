export interface SellerFormValues {
  name: string;
  email: string;
  phone: string;
  commissionPercentage: number;
  active: boolean;
}

export const emptySellerForm: SellerFormValues = {
  name: '',
  email: '',
  phone: '',
  commissionPercentage: 5,
  active: true,
};

export function validateSellerForm(form: SellerFormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!form.name?.trim()) errors.name = 'Nombre obligatorio';
  return errors;
}
