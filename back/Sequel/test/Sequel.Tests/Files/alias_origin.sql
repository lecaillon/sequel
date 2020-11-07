select x.id, y.name, z.label
from table1 x
	inner join schema2.table2 as  y
		on x.id = y.id
	inner join
	(
		select z.id, age as name, count(*) as total
		from ages z
	) as z
        on z.id = y.id
where	x.id > 0
and y.id > 0
or a.id = 1;
