SELECT object_definition(object_id) AS Definition FROM sys.triggers WHERE parent_id = OBJECT_ID('DonDatLich');
