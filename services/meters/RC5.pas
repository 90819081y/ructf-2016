unit RC5;

{$mode objfpc}{$H+}

interface
	uses
		fgl, SysUtils;

	type
		TList = specialize TFPGList<QWord>;

	function unpack(const qq: qword): TDateTime;

	function appendBlock(const prefix: string; const qq: qword): string;
	function appendBlock(const prefix: string; const t: TDateTime): string;

	function encodeBlock(const qq: qword): string;
	function encodeBlock(const t: TDateTime): string;

	function decode(const txt: string): TList;

implementation

	const
		p: dword = $B7E15163;
		q: dword = $9E3779B9;
		r = 0;
		rr = 2 * (r + 1);
		key = 'Very secret key! Don''t say anybody!!';
		c = length(key) div 4;

	var
		s: array [0 .. rr - 1] of dword;

	function pack(const a, b: dword): Qword;
	begin
		result := b;
		result := (result shl 32) + a;
	end;

	function pack(const ss: string; const size: longint = 8): QWord;
	var
		i: byte;
	begin
		result := 0;
		for i := 0 to size - 1 do
			result := (result * 256) + ord(ss[length(ss) - i]);
	end;

	function sunpack(a: dword): string; 
	var
		i: byte;
	begin
		result := '';
		for i := 1 to 4 do
		begin
			result := result + char(a mod 256);
			a := a div 256;
		end;
	end;

	function unpack(const qq: qword): TDateTime;
	var
		ts: TTimeStamp;
	begin
		ts.Date := qq;
		ts.Time := qq shr 32;
		result := TimeStampToDateTime(ts);
	end;

	function rol(a, sh: dword): dword;
	begin
		sh := sh mod 32;
		result := (a shl sh) or (a shr (32 - sh));
	end;

	function encodeBlock(a, b: dword): string;
	var
		i: longint;
	begin
		a := a + s[0];
		b := b + s[1];

		for i := 1 to r do
		begin
			a := rol(a xor b, b) + s[2 * i];
			b := rol(b xor a, a) + s[2 * i + 1];
		end;

		result := sunpack(a) + sunpack(b);
	end;

	function encodeBlock(const qq: qword): string;
	begin
		 result := encodeBlock(qq, qq shr 32);
	end;

	function encodeBlock(const t: TDateTime): string;
	var
		ts: TTimeStamp;
	begin
		ts := DateTimeToTimeStamp(t);
		result := encodeBlock(ts.date, ts.time)
	end;

	function appendBlock(const prefix: string; const qq: qword): string;
	begin
		result := prefix + encodeBlock(pack(prefix) xor qq);
	end;

	function appendBlock(const prefix: string; const a, b: dword): string;
	begin
		result := appendBlock(prefix, pack(a, b));
	end;

	function appendBlock(const prefix: string; const t: TDateTime): string;
	var
		ts: TTimeStamp;
	begin
		ts := DateTimeToTimeStamp(t);
		result := appendBlock(prefix, ts.date, ts.time)
	end;

	function ror(a, sh: dword): dword;
	begin
		sh := sh mod 32;
		result := (a shr sh) or (a shl (32 - sh));
	end;

	function decodeBlock(const qq: qword): qword;
	var
		i: longint;
		a, b: dword;
	begin
		a := qq;
		b := qq shr 32;

		for i := r downto 1 do
		begin
			b := ror(b - s[2 * i + 1], a) xor a;
			a := ror(a - s[2 * i], b) xor b;
		end;

		b := b - s[1];
		a := a - s[0];

		result := pack(a, b);
	end;


	function decode(const txt: string): TList;
	var
		i: longint;
		qq, pp: qword;
	begin
		result := TList.Create;

		qq := 0;
		for i := 0 to length(txt) div 8 - 1 do
		begin
			pp := pack(copy(txt, 8 * i + 1, 8));
			result.add(decodeBlock(pp) xor qq);
			qq := pp;
		end;
	end;

	function max(const a, b: dword): dword;
	begin
		if a < b then
			result := b
		else
			result := a;
	end;

	procedure expansionKey;
	var
		l: array [0 .. c - 1] of dword;
		i, j, g, h, k: dword;
	begin
		for i := 0 to c - 1 do
			l[i] := pack(copy(key, 4 * i + 1, 4), 4);

		s[0] := p;
		for i := 1 to rr - 1 do
			s[i] := s[i - 1] + q;
	
		g := 0;
		h := 0;
		i := 0;
		j := 0;

		for k := 1 to max(3 * c, 3 * rr) do
		begin
			g := rol(s[i] + g + h, 3);
			s[i] := g;
			h := rol(l[j] + g + h, g + h);
			l[j] := h;
			i := (i + 1) mod rr;
			j := (j + 1) mod c;
		end;
	end;

initialization
	writeln(stderr, 'initialization RC5');
	flush(stderr);
	expansionKey;

end.
