﻿CREATE OR REPLACE FUNCTION foo() RETURNS integer AS
$body$
DECLARE
BEGIN
 select * from foo;
END;
$body$
LANGUAGE 'plpgsql' VOLATILE CALLED ON NULL INPUT SECURITY INVOKER;