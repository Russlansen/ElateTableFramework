IF EXISTS (SELECT * FROM dbo.sysobjects WHERE id = object_id('[Autos]') AND OBJECTPROPERTY(id, 'IsUserTable') = 1) 
DROP TABLE [Autos]
;

CREATE TABLE [Autos]
(
	[Id] int NOT NULL IDENTITY (1, 1),
	[Model] varchar(max),
	[Engine] varchar(max),
	[Year] varchar(max),
	[Color] varchar(max),
	[Price] varchar(max),
	[Date] datetime
)
;

ALTER TABLE [Autos] 
 ADD CONSTRAINT [PK_Autos]
	PRIMARY KEY CLUSTERED ([Id])
;
