import {index, integer, real, sqliteTable, text, uniqueIndex} from 'drizzle-orm/sqlite-core';

export const clientsTable = sqliteTable('clients', {
  id: text('id').primaryKey(),
  name: text('name').notNull(),
  phone: text('phone').notNull().default(''),
  address: text('address').notNull().default(''),
  postalCode: text('postal_code').notNull().default(''),
  street: text('street').notNull().default(''),
  complement: text('complement').notNull().default(''),
  neighborhood: text('neighborhood').notNull().default(''),
  city: text('city').notNull().default(''),
  state: text('state').notNull().default(''),
  observations: text('observations').notNull().default(''),
  sortOrder: integer('sort_order').notNull().default(0),
});

export const roomsTable = sqliteTable('rooms', {
  id: text('id').primaryKey(),
  clientId: text('client_id').notNull().references(() => clientsTable.id, {onDelete: 'cascade'}),
  name: text('name').notNull(),
  imageLabel: text('image_label').notNull().default('Sem fotos'),
  sortOrder: integer('sort_order').notNull().default(0),
}, table => [index('rooms_client_id_idx').on(table.clientId)]);

export const recordsTable = sqliteTable('visit_records', {
  id: text('id').primaryKey(),
  roomId: text('room_id').notNull().references(() => roomsTable.id, {onDelete: 'cascade'}),
  createdAt: text('created_at').notNull(),
  notes: text('notes').notNull().default(''),
  roomNotes: text('room_notes').notNull().default(''),
  openingName: text('opening_name').notNull().default(''),
  widthCentimeters: real('width_centimeters').notNull().default(0),
  heightCentimeters: real('height_centimeters').notNull().default(0),
  productCategory: text('product_category').notNull().default('Cortinas'),
  modelName: text('model_name').notNull().default('Cortina sob medida'),
  panelType: text('panel_type').notNull().default('DoublePanel'),
  fabricMaterialName: text('fabric_material_name').notNull().default('CurtainFabric'),
  rodMaterialName: text('rod_material_name').notNull().default('RodMaterial'),
  sortOrder: integer('sort_order').notNull().default(0),
}, table => [index('records_room_id_idx').on(table.roomId)]);

export const photosTable = sqliteTable('photos', {
  id: text('id').primaryKey(),
  clientId: text('client_id').notNull().references(() => clientsTable.id, {onDelete: 'cascade'}),
  roomId: text('room_id').references(() => roomsTable.id, {onDelete: 'cascade'}),
  recordId: text('record_id').references(() => recordsTable.id, {onDelete: 'cascade'}),
  path: text('path').notNull(),
  roomName: text('room_name').notNull().default(''),
  capturedAt: text('captured_at'),
  sortOrder: integer('sort_order').notNull().default(0),
}, table => [
  uniqueIndex('photos_path_unique').on(table.path),
  index('photos_client_id_idx').on(table.clientId),
  index('photos_room_id_idx').on(table.roomId),
  index('photos_record_id_idx').on(table.recordId),
]);

export const appMetaTable = sqliteTable('app_meta', {
  key: text('key').primaryKey(),
  value: text('value').notNull(),
});

export const schema = {
  clients: clientsTable,
  rooms: roomsTable,
  records: recordsTable,
  photos: photosTable,
  appMeta: appMetaTable,
};
