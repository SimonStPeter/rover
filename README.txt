
I chose to take longer over this than the recommended time, but this was extended considerably because I pulled my back over the weekend and could only work for short periods before stopping, so this has been spread out over two days.
I'm not the fastest programmer but this is not typical!

Regarding this specification, it was unclear what you preferred: a rough and ready prototype or something that would indicate how a more realistic, larger program would be structured. I took the latter view, which means that this relatively straightforward demo has got a whole load of machinery that is simply inappropriate for something so small.

If I'd done this as a simple prototype, the movement's file validation would have been minimal, the directions classes would have simply been a small hash table of tuples, and the position would just have been a pair of integers. Rover and Map would be small objects but that's all.

Instead this has large amounts of object orientation, which works very successfully for larger programs I've worked on inc. my own. Some additional machinery has been added from my own personal project, these being 'must' and ExpectException which take type parameters and allow for exception checking in unit testing.
My personal project has several hundred such unit tests and these are invaluable there, but again, if this were not a demo I would not have taken this approach. I am not an architecture astronaut, I will normally use the simplest appropriate solution.

To be really clear, this was done just for demonstration purposes.

Mutability is quite popular and I rely on it heavily, but Rover is not entirely immutable.

This is done pretty much in my own personal style which I am happy to change if you prefer. I don't have any particular feelings about coding standards, I use what I feel is short and conveys information well, but I'm always open to improvements. I do like spacing things out though.

The output is to console as I have no HTML/CSS or web tech, which you and the recruiter are aware of.