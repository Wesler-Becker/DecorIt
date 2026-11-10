CREATE TABLE `app_meta` (
	`key` text PRIMARY KEY NOT NULL,
	`value` text NOT NULL
);
--> statement-breakpoint
CREATE TABLE `clients` (
	`id` text PRIMARY KEY NOT NULL,
	`name` text NOT NULL,
	`phone` text DEFAULT '' NOT NULL,
	`address` text DEFAULT '' NOT NULL,
	`postal_code` text DEFAULT '' NOT NULL,
	`street` text DEFAULT '' NOT NULL,
	`complement` text DEFAULT '' NOT NULL,
	`neighborhood` text DEFAULT '' NOT NULL,
	`city` text DEFAULT '' NOT NULL,
	`state` text DEFAULT '' NOT NULL,
	`observations` text DEFAULT '' NOT NULL,
	`sort_order` integer DEFAULT 0 NOT NULL
);
--> statement-breakpoint
CREATE TABLE `photos` (
	`id` text PRIMARY KEY NOT NULL,
	`client_id` text NOT NULL,
	`room_id` text,
	`record_id` text,
	`path` text NOT NULL,
	`room_name` text DEFAULT '' NOT NULL,
	`captured_at` text,
	`sort_order` integer DEFAULT 0 NOT NULL,
	FOREIGN KEY (`client_id`) REFERENCES `clients`(`id`) ON UPDATE no action ON DELETE cascade,
	FOREIGN KEY (`room_id`) REFERENCES `rooms`(`id`) ON UPDATE no action ON DELETE cascade,
	FOREIGN KEY (`record_id`) REFERENCES `visit_records`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE UNIQUE INDEX `photos_path_unique` ON `photos` (`path`);--> statement-breakpoint
CREATE INDEX `photos_client_id_idx` ON `photos` (`client_id`);--> statement-breakpoint
CREATE INDEX `photos_room_id_idx` ON `photos` (`room_id`);--> statement-breakpoint
CREATE INDEX `photos_record_id_idx` ON `photos` (`record_id`);--> statement-breakpoint
CREATE TABLE `visit_records` (
	`id` text PRIMARY KEY NOT NULL,
	`room_id` text NOT NULL,
	`created_at` text NOT NULL,
	`notes` text DEFAULT '' NOT NULL,
	`room_notes` text DEFAULT '' NOT NULL,
	`opening_name` text DEFAULT '' NOT NULL,
	`width_centimeters` real DEFAULT 0 NOT NULL,
	`height_centimeters` real DEFAULT 0 NOT NULL,
	`product_category` text DEFAULT 'Cortinas' NOT NULL,
	`model_name` text DEFAULT 'Cortina sob medida' NOT NULL,
	`panel_type` text DEFAULT 'DoublePanel' NOT NULL,
	`fabric_material_name` text DEFAULT 'CurtainFabric' NOT NULL,
	`rod_material_name` text DEFAULT 'RodMaterial' NOT NULL,
	`sort_order` integer DEFAULT 0 NOT NULL,
	FOREIGN KEY (`room_id`) REFERENCES `rooms`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE INDEX `records_room_id_idx` ON `visit_records` (`room_id`);--> statement-breakpoint
CREATE TABLE `rooms` (
	`id` text PRIMARY KEY NOT NULL,
	`client_id` text NOT NULL,
	`name` text NOT NULL,
	`image_label` text DEFAULT 'Sem fotos' NOT NULL,
	`sort_order` integer DEFAULT 0 NOT NULL,
	FOREIGN KEY (`client_id`) REFERENCES `clients`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE INDEX `rooms_client_id_idx` ON `rooms` (`client_id`);