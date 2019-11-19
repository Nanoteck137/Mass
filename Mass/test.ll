; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

declare i8* @malloc(i64)

declare void @free(i8*)

define void @test(i8* %format, i8 %a, ...) {
entry:
  %format1 = alloca i8*
  store i8* %format, i8** %format1
  %a2 = alloca i8
  store i8 %a, i8* %a2
  ret void
}

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %a = alloca float
  store float 0x40091EB860000000, float* %a
  ret i32 0
}
