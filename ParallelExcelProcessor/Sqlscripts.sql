CREATE TABLE AllDataTypes ( IntegerField INT NULL,
StringField VARCHAR(255) NULL,
DateField Datetime NULL,
FloatField FLOAT NULL,
BooleanField bit NULL );

CREATE TYPE AllDataTypes_Type AS TABLE ( IntegerField INT NULL,
StringField VARCHAR(255) NULL,
DateField Datetime NULL,
FloatField FLOAT NULL,
BooleanField bit NULL );

CREATE PROCEDURE upsertalldatatypes_procedure @AllDataTypesTable AllDataTypes_Type READONLY AS BEGIN SET
nocount ON;

MERGE dbo.AllDataTypes AS target
	USING @AllDataTypesTable AS SOURCE ON
target.IntegerField = SOURCE.IntegerField
WHEN MATCHED THEN
UPDATE
SET
	IntegerField = SOURCE.IntegerField,
	StringField = SOURCE.StringField,
	DateField = SOURCE.DateField,
	FloatField = SOURCE.FloatField,
	BooleanField = SOURCE.BooleanField
	WHEN NOT MATCHED THEN
INSERT
	(IntegerField,
	StringField,
	DateField,
	FloatField,
	BooleanField)
VALUES (SOURCE.IntegerField,
SOURCE.StringField,
SOURCE.DateField,
SOURCE.FloatField,
SOURCE.BooleanField);

END

