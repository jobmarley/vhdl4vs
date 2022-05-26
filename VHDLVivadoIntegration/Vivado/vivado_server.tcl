
puts "Connecting to localhost:9900..."
set channel [socket localhost 9900]
puts "Connected."
while { [gets $channel line] >= 0 } {
	puts "Received line: $line"

	set args [split $line]
	if { [llength $args] < 1 } {
		puts "Invalid command, length < 1"
		continue
	}
		
	set result ""

	if { [lindex $args 0] == "execute" } {
		set result [eval [lreplace $args 0 0]]
	} else {
		puts "Unknown command: [lindex $args 0]"
		continue
	}
	puts $channel $result
	flush $channel
}
puts "closing channel"
close $channel