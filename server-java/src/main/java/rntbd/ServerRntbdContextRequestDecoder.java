package rntbd;

import io.netty.buffer.ByteBuf;
import io.netty.channel.ChannelHandlerContext;
import io.netty.handler.codec.ByteToMessageDecoder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.List;

/**
 * The methods included in this class are copied from {@link ServerRntbdContextRequestDecoder}.
 */
public class ServerRntbdContextRequestDecoder extends ByteToMessageDecoder {
    private final static Logger logger = LoggerFactory.getLogger(ServerRntbdContextRequestDecoder.class);

    public ServerRntbdContextRequestDecoder() {
        this.setSingleDecode(true);
    }

    /**
     * Prepare for decoding an @{link RntbdContextRequest} or fire a channel readTree event to pass the input message along
     *
     * @param context the {@link ChannelHandlerContext} which this {@link ByteToMessageDecoder} belongs to
     * @param message the message to be decoded
     * @throws Exception thrown if an error occurs
     */
    @Override
    public void channelRead(final ChannelHandlerContext context, final Object message) throws Exception {
        logger.warn("Got channelRead with ctx: {} for type {} Hashcode: {}", context.name(), message.getClass().getName(), message.hashCode());

        if (message instanceof ByteBuf) {

            final ByteBuf in = (ByteBuf)message;
            final int resourceOperationType = in.getInt(in.readerIndex() + Integer.BYTES);

            if (resourceOperationType == 0) {
                assert this.isSingleDecode();
                super.channelRead(context, message);
                return;
            }
        }
        context.fireChannelRead(message);
    }

    /**
     * Decode an RntbdContextRequest from an {@link ByteBuf} stream
     * <p>
     * This method will be called till either an input {@link ByteBuf} has nothing to readTree on return from this method or
     * till nothing is readTree from the input {@link ByteBuf}.
     *
     * @param context the {@link ChannelHandlerContext} which this {@link ByteToMessageDecoder} belongs to
     * @param in      the {@link ByteBuf} from which to readTree data
     * @param out     the {@link List} to which decoded messages should be added
     * @throws IllegalStateException thrown if an error occurs
     */
    @Override
    protected void decode(final ChannelHandlerContext context, final ByteBuf in, final List<Object> out) throws IllegalStateException {
        logger.warn("decode called with ctx: {} Hashcode: {}", context.name(), in.hashCode());

        final RntbdContextRequest request;
        in.markReaderIndex();

        try {
            request = RntbdContextRequest.decode(in);
        } catch (final IllegalStateException error) {
            logger.warn("decode failed with {}", error);
            in.resetReaderIndex();
            throw error;
        }

        in.discardReadBytes();
        out.add(request);
    }
}
