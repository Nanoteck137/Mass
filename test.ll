; ModuleID = 'NO NAME'
source_filename = "NO NAME"

%struct.FILE = type opaque

@str = private unnamed_addr constant [7 x i8] c"Equal\0A\00", align 1

declare i32 @printf(i8*, ...)

declare %struct.FILE* @fopen(i8*, i8*)

declare i64 @fwrite(i8*, i64, i64, %struct.FILE*)

declare i32 @fclose(%struct.FILE*)

declare i64 @strlen(i8*)

define i32 @main(i32 %argc, i8** %argv) {
entry:
  %argc1 = alloca i32
  store i32 %argc, i32* %argc1
  %argv2 = alloca i8**
  store i8** %argv, i8*** %argv2
  %a = alloca i32
  store i32 4, i32* %a
  %b = alloca i32
  store i32 5, i32* %b
  %0 = load i32, i32* %a
  %1 = icmp eq i32 %0, 3
  %2 = xor i1 %1, true
  br i1 %2, label %then, label %endif

then:                                             ; preds = %entry
  %3 = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([7 x i8], [7 x i8]* @str, i32 0, i32 0))
  br label %endif

endif:                                            ; preds = %then, %entry
  ret i32 0
}
