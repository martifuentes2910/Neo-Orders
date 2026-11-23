import { z } from 'zod';

export const clientSchema = z.object({
  nombre: z.string().min(2, 'Nombre inválido'),
  email: z.string().email('Email inválido'),
  telefono: z.string().optional().or(z.literal('')),
  direccion: z.string().optional().or(z.literal('')),
});

export type ClientFormValues = z.infer<typeof clientSchema>;

export const emptyClientForm: ClientFormValues = { nombre: '', email: '', telefono: '', direccion: '' };
