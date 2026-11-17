CREATE TABLE `record_curtain_settings` (
	`record_id` text NOT NULL,
	`layer_index` integer NOT NULL,
	`field` text NOT NULL,
	`value` text NOT NULL,
	`value_type` text NOT NULL,
	FOREIGN KEY (`record_id`) REFERENCES `visit_records`(`id`) ON UPDATE no action ON DELETE cascade
);
--> statement-breakpoint
CREATE UNIQUE INDEX `record_curtain_settings_key` ON `record_curtain_settings` (`record_id`,`layer_index`,`field`);