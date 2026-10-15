import {composeClientAddress, formatBrazilianPhone, formatBrazilianPostalCode} from './clientForm';

describe('máscaras do cadastro de cliente', () => {
  it('formata telefone e limita a 11 dígitos numéricos', () => {
    expect(formatBrazilianPhone('11987654321')).toBe('(11) 98765-4321');
    expect(formatBrazilianPhone('(11) 98765-432199')).toBe('(11) 98765-4321');
    expect(formatBrazilianPhone('abc')).toBe('');
  });

  it('formata CEP e limita a 8 dígitos', () => {
    expect(formatBrazilianPostalCode('95905022')).toBe('95905-022');
    expect(formatBrazilianPostalCode('95905-02299')).toBe('95905-022');
  });

  it('monta endereço compatível com os pontos de exibição já existentes', () => {
    expect(composeClientAddress({postalCode: '95905-022', street: 'Rua Albino', complement: 'Casa 2', neighborhood: 'Montanha', city: 'Lajeado', state: 'Rio Grande do Sul'}))
      .toBe('Rua Albino, Casa 2, Montanha, Lajeado - Rio Grande do Sul, 95905-022');
  });
});
