local abs, log, sin, floor = math.abs, math.log, math.sin, math.floor
--local abs = math.abs
local format = string.format

local function printf(...)
  io.write(format(...))
end
printf("Hello")

