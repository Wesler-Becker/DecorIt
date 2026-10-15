import type {ClientData} from '../types';

export function formatBrazilianPhone(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 11);
  if (!digits) return '';
  const ddd = digits.slice(0, 2);
  const number = digits.slice(2);
  if (digits.length <= 2) return `(${ddd}`;
  if (number.length <= 4) return `(${ddd}) ${number}`;
  if (number.length <= 8) return `(${ddd}) ${number.slice(0, 4)}-${number.slice(4)}`;
  return `(${ddd}) ${number.slice(0, 5)}-${number.slice(5)}`;
}

export function formatBrazilianPostalCode(value: string): string {
  const digits = value.replace(/\D/g, '').slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

export function composeClientAddress(client: Pick<ClientData, 'postalCode' | 'street' | 'complement' | 'neighborhood' | 'city' | 'state'>): string {
  const locality = [client.city.trim(), client.state.trim()].filter(Boolean).join(' - ');
  return [client.street, client.complement, client.neighborhood, locality, client.postalCode]
    .map(value => value.trim())
    .filter(Boolean)
    .join(', ');
}
